
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Stores;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Configuration;
#endif

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Represents an error log capable of storing and retrieving errors generated in an ASP.NET Web application.
    /// </summary>
    public abstract class ErrorStore
    {
        private static ErrorStore _defaultStore;
        private static ConcurrentQueue<Error> _writeQueue;
        private static Thread _retryThread;
        private static readonly object _retryLock = new object();
        private static bool _isInRetry;
        private Exception _retryException;

        /// <summary>
        /// The settings for this store.
        /// </summary>
        public ErrorStoreSettings Settings { get; }

        /// <summary>
        /// Base constructor of the error store to set common properties.
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>     
        protected ErrorStore(ErrorStoreSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Gets if this error store is in failure mode (retrying to log in an interval).
        /// </summary>
        public bool InFailureMode => _isInRetry;

        /// <summary>
        /// The last time this error store failed to write an error.
        /// </summary>
        protected DateTime? LastWriteFailure;

        /// <summary>
        /// Logs an error in log for the application.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected abstract void LogError(Error error);

        /// <summary>
        /// Retrieves a single error based on Id.
        /// </summary>
        /// <param name="guid">The GUID of the error to retrieve.</param>
        protected abstract Task<Error> GetErrorAsync(Guid guid);

        /// <summary>
        /// Prevents error identified by <paramref name="guid"/> from being deleted when the error log is full, if the store supports it.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect.</param>
        protected abstract Task<bool> ProtectErrorAsync(Guid guid);

        /// <summary>
        /// Protects a list of errors in the log.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to protect.</param>
        protected virtual async Task<bool> ProtectErrorsAsync(IEnumerable<Guid> guids)
        {
            if (guids == null) return false;
            var success = true;
            foreach (var guid in guids)
            {
                if (!await ProtectErrorAsync(guid).ConfigureAwait(false))
                {
                    success = false;
                }
            }
            return success;
        }

        /// <summary>
        /// Deletes a specific error from the log.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        protected abstract Task<bool> DeleteErrorAsync(Guid guid);

        /// <summary>
        /// Deletes a list of errors from the log, only if they are not protected.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to delete.</param>
        protected virtual async Task<bool> DeleteErrorsAsync(IEnumerable<Guid> guids)
        {
            if (guids == null) return false;
            var success = true;
            foreach (var guid in guids)
            {
                if (!await DeleteErrorAsync(guid).ConfigureAwait(false))
                {
                    success = false;
                }
            }
            return success;
        }

        /// <summary>
        /// Deletes a specific error from the log, any traces of it.
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> ID of the error to hard delete.</param>
        protected virtual Task<bool> HardDeleteErrorAsync(Guid guid) => DeleteErrorAsync(guid);

        /// <summary>
        /// Deletes all non-protected errors from the log.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        protected abstract Task<bool> DeleteAllErrorsAsync(string applicationName = null);

        /// <summary>
        /// Retrieves all of the errors in the log.
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        protected abstract Task<List<Error>> GetAllErrorsAsync(string applicationName = null);

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if <c>null</c>.
        /// </summary>
        /// <param name="since">The date to get errors since.</param>
        /// <param name="applicationName">The application name to get an error count for.</param>
        protected abstract Task<int> GetErrorCountAsync(DateTime? since = null, string applicationName = null);

        /// <summary>
        /// Get the name of this error log store implementation.
        /// </summary>
        public virtual string Name => GetType().Name;

        private static string _applicationName { get; set; }
        /// <summary>
        /// Gets the name of the application to which the log is scoped.
        /// </summary>
        public static string ApplicationName => _applicationName ?? (_applicationName = Exceptional.Settings.Current.ApplicationName);

        /// <summary>
        /// Gets the name of the machine logging these errors.
        /// </summary>
        public virtual string MachineName => Environment.MachineName;

        /// <summary>
        /// Gets the default error store specified in the configuration, 
        /// or the in-memory store if none is configured.
        /// </summary>
        public static ErrorStore Default => _defaultStore ?? (_defaultStore = GetErrorStoreFromConfig());

        /// <summary>
        /// Sets the default error store to use for logging
        /// </summary>
        /// <param name="applicationName">The application name to use when logging errors</param>
        /// <param name="store">The error store used to store, e.g. <code>new SQLErrorStore(myConnectionString)</code></param>
        public static void Setup(string applicationName, ErrorStore store)
        {
            _defaultStore = store;
            _applicationName = applicationName;
        }

        /// <summary>
        /// Gets the write queue for errors, which is populated in the case of a write failure.
        /// </summary>
        public static ConcurrentQueue<Error> WriteQueue => _writeQueue ?? (_writeQueue = new ConcurrentQueue<Error>());

        /// <summary>
        /// Gets the last exception that happened when trying to log exceptions.
        /// </summary>
        public Exception LastRetryException => _retryException;

        /// <summary>
        /// Logs an error in log for the application.
        /// </summary>
        /// <param name="error">The error to log.</param>
        public void Log(Error error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));

            // Track the GUID we made vs. what the store returns. If it's different, it's a dupe.
            var originalGuid = error.GUID;
            // if we're in a retry state, log directly to the queue
            if (_isInRetry)
            {
                QueueError(error);
                if (originalGuid != error.GUID) error.IsDuplicate = true;
                NotifyAll(error);
                return;
            }
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    LogError(error);
                }
                if (originalGuid != error.GUID) error.IsDuplicate = true;
                NotifyAll(error);
            }
            catch (Exception ex)
            {
                _retryException = ex;
                // if we fail to write the error to the store, queue it for re-writing
                QueueError(error);
            }
        }

        /// <summary>
        /// Notify everything currently registered as a notifier.
        /// </summary>
        /// <param name="error">The error to notify things of.</param>
        private void NotifyAll(Error error)
        {
            var settings = Exceptional.Settings.Current;
            if (settings.Notifiers.Count > 0)
            {
                foreach (var n in settings.Notifiers)
                {
                    try
                    {
                        if (n?.Enabled == true)
                        {
                            n.Notify(error);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("Error in NotifyAll: " + e);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes all non-protected errors from the log.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect.</param>
        public async Task<bool> ProtectAsync(Guid guid)
        {
            if (_isInRetry) return false; // no protecting allowed when failing, since we don't respect it in the queue anyway

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                return await ProtectErrorAsync(guid).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Protects a list of errors in the log.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to protect.</param>
        public async Task<bool> ProtectAsync(IEnumerable<Guid> guids)
        {
            if (guids == null) return false;
            if (_isInRetry) return false; // no protecting allowed when failing, since we don't respect it in the queue anyway

            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    return await ProtectErrorsAsync(guids).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes an error from the log with the specified <paramref name="guid"/>.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        public async Task<bool> DeleteAsync(Guid guid)
        {
            if (_isInRetry) return false; // no deleting from the retry queue

            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    return await DeleteErrorAsync(guid).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes a list of non-protected errors from the log.
        /// </summary>
        /// <param name="guids">The GUIDs of the errors to delete.</param>
        public async Task<bool> DeleteAsync(IEnumerable<Guid> guids)
        {
            if (guids == null) return false;
            if (_isInRetry) return false; // no deleting from the retry queue

            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    return await DeleteErrorsAsync(guids).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes all non-protected errors from the log.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        public async Task<bool> DeleteAllAsync(string applicationName = null)
        {
            if (_isInRetry)
            {
                _writeQueue = new ConcurrentQueue<Error>();
                return true;
            }

            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    return await DeleteAllErrorsAsync(applicationName).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a specific exception with the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the error to retrieve.</param>
        public async Task<Error> GetAsync(Guid guid)
        {
            if (_isInRetry)
            {
                return WriteQueue.FirstOrDefault(e => e.GUID == guid);
            }

            try { return await GetErrorAsync(guid).ConfigureAwait(false); }
            catch (Exception ex) { BeginRetry(ex); }
            return null;
        }

        /// <summary>
        /// Gets all in the store, including those in the backup queue if it's in use.
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        public async Task<List<Error>> GetAllAsync(string applicationName = null)
        {
            if (_isInRetry)
            {
                // Dupe it!
                return new List<Error>(WriteQueue);
            }

            try { return await GetAllErrorsAsync(applicationName).ConfigureAwait(false); }
            catch (Exception ex) { BeginRetry(ex); }
            return new List<Error>();
        }

        /// <summary>
        /// Gets the count of exceptions, optionally those since a certain date.
        /// </summary>
        /// <param name="since">The minimum date to fetch errors after.</param>
        /// <param name="applicationName">The application name to fetch errors for.</param>
        public async Task<int> GetCountAsync(DateTime? since = null, string applicationName = null)
        {
            if (_isInRetry)
            {
                return WriteQueue.Count;
            }

            try { return await GetErrorCountAsync(since, applicationName).ConfigureAwait(false); }
            catch (Exception ex) { BeginRetry(ex); }
            return 0;
        }

        /// <summary>
        /// Queues an error into the backup/retry queue.
        /// </summary>
        /// <param name="e">The error to queue for writing.</param>
        /// <remarks>These will be written to the store when we're able to connect again.</remarks>
        protected void QueueError(Error e)
        {
            // try and roll-up in the queue, to save space
            foreach (var err in WriteQueue.Where(err => e.ErrorHash == err.ErrorHash))
            {
                e.GUID = err.GUID;
                err.DuplicateCount++;
                return;
            }

            // only queue if we're under the cap
            if (WriteQueue.Count < Settings.BackupQueueSize)
                WriteQueue.Enqueue(e);

            // spin up the retry mechanism
            BeginRetry();
        }

        private void BeginRetry(Exception ex = null)
        {
            lock (_retryLock)
            {
                if (ex != null) _retryException = ex;
                _isInRetry = true;

                // are we already spun up?
                if (_retryThread?.IsAlive == true) return;

                _retryThread = new Thread(TryFlushQueue);
                _retryThread.Start();
            }
        }

        private async void TryFlushQueue()
        {
            if (!_isInRetry && WriteQueue.IsEmpty) return;

            while (true)
            {
                Thread.Sleep(Settings.BackupQueueRetryInterval);

                // if the error store is still down, sleep again
                if (!(await Default.TestAsync().ConfigureAwait(false))) continue;

                // empty queue
                while (!WriteQueue.IsEmpty)
                {
                    // if we can't pop one off, get out of here
                    if (!WriteQueue.TryDequeue(out Error e)) return;

                    try
                    {
                        Default.LogError(e);
                    }
                    catch
                    {
                        // if we had an error logging, stick it back in the queue and jump out, else we'll iterate this thing forever
                        Default.QueueError(e);
                        break;
                    }
                }
                // if we emptied the queue, return to a normal state
                if (WriteQueue.IsEmpty)
                {
                    _isInRetry = false;
                    TryFlushQueue(); // clear out any that may have come in due to thread races
                    return;
                }
            }
        }

        /// <summary>
        /// Tests to see if this error store is working.
        /// </summary>
        public async Task<bool> TestAsync()
        {
            try
            {
                var error = new Error(new Exception("Test Exception"));
                LogError(error);
                await HardDeleteErrorAsync(error.GUID).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                return false;
            }
        }

        private static ErrorStore GetErrorStoreFromConfig()
        {
            return GetFromSettings(Exceptional.Settings.Current.Store) ?? new MemoryErrorStore();
        }

        private static ErrorStore GetFromSettings(ErrorStoreSettings settings)
        {
            if (settings == null) return null;

            // a bit of validation
            if (settings.Type.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "ErrorStore 'type' must be specified");
            if (settings.Size < 1)
                throw new ArgumentOutOfRangeException(nameof(settings), "ErrorStore 'size' must be at least 1");

            var storeTypes = GetErrorStores();
            // Search by convention first
            // or...free for all!
            Type match = storeTypes.Find(s => s.Name == settings.Type + nameof(ErrorStore)) ?? storeTypes.Find(s => s.Name.Contains(settings.Type));

            if (match == null)
            {
                throw new Exception("Could not find error store type: " + settings.Type);
            }

            try
            {
                return (ErrorStore) Activator.CreateInstance(match, settings);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating a " + settings.Type + " error store: " + ex.Message, ex);
            }
        }

        private static List<Type> GetErrorStores()
        {
            try
            {
                // Ensure all assemblies we expect are loaded before looking at types
                var path = Path.GetDirectoryName(typeof(ErrorStore).Assembly.Location);
                foreach (var filename in Directory.GetFiles(path, "StackExchange.Exceptional.*.dll"))
                {
                    Assembly.LoadFrom(filename);
                }

                // Check for any implementers of ErrorStore anywhere
                return AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(s => s.GetTypes())
                                .Where(t => t.IsSubclassOf(typeof(ErrorStore)))
                                .ToList();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error loading error stores: " + ex.Message);
            }
            return new List<Type>();
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Gets the connection string from the connectionStrings configuration element, from web.config or app.config, throws if not found.
        /// </summary>
        /// <param name="connectionStringName">The connection string name to fetch</param>
        /// <returns>The connection string requested</returns>
        /// <exception cref="ConfigurationErrorsException">Connection string was not found</exception>
        protected static string GetConnectionStringByName(string connectionStringName)
        {
            if (connectionStringName.IsNullOrEmpty()) return null;

            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (connectionString == null)
                throw new ConfigurationErrorsException("A connection string was not found for the connection string name provided");
            return connectionString.ConnectionString;
        }
#endif
    }
}