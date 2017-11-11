using StackExchange.Exceptional.Stores;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Transactions;
using System.Threading.Tasks;
using StackExchange.Exceptional.Internal;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Represents an error log capable of storing and retrieving errors generated in an ASP.NET Web application.
    /// </summary>
    public abstract class ErrorStore
    {
        private ConcurrentQueue<Error> _writeQueue;
        private Thread _retryThread;
        private readonly object _retryLock = new object();
        private bool _isInRetry;
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
        protected abstract bool LogError(Error error);

        /// <summary>
        /// Asynchronously logs an error in log for the application.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected virtual Task<bool> LogErrorAsync(Error error) => Task.FromResult(LogError(error));

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

        /// <summary>
        /// Gets the name of the application to which the log is scoped.
        /// </summary>
        public string ApplicationName => Settings.ApplicationName;

        /// <summary>
        /// Gets the write queue for errors, which is populated in the case of a write failure.
        /// </summary>
        public ConcurrentQueue<Error> WriteQueue => _writeQueue ?? (_writeQueue = new ConcurrentQueue<Error>());

        /// <summary>
        /// Gets the last exception that happened when trying to log exceptions.
        /// </summary>
        public Exception LastRetryException => _retryException;

        private bool QueuedInRetry(Error error, Guid originalGuid)
        {
            if (_isInRetry)
            {
                QueueError(error);
                ProcessNotifications(error, originalGuid);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Notify everything currently registered as a notifier.
        /// </summary>
        /// <param name="error">The error to notify things of.</param>
        /// <param name="originalGuid">The GUID of the original error, for determining if this is a duplicate.</param>
        private void ProcessNotifications(Error error, Guid originalGuid)
        {
            if (originalGuid != error.GUID) error.IsDuplicate = true;

            if (error.Settings.Notifiers.Count > 0)
            {
                foreach (var n in error.Settings.Notifiers)
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
        /// Logs an error in log for the application.
        /// </summary>
        /// <param name="error">The error to log.</param>
        public bool Log(Error error)
        {
            _ = error ?? throw new ArgumentNullException(nameof(error));

            // Track the GUID we made vs. what the store returns. If it's different, it's a dupe.
            var originalGuid = error.GUID;
            // if we're in a retry state, log directly to the queue
            if (QueuedInRetry(error, originalGuid))
            {
                return false;
            }
            try
            {
                bool result;
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    result = LogError(error);
                }
                ProcessNotifications(error, originalGuid);
                return result;
            }
            catch (Exception ex)
            {
                _retryException = ex;
                // if we fail to write the error to the store, queue it for re-writing
                QueueError(error);
                return false;
            }
        }

        /// <summary>
        /// Logs an error in log for the application.
        /// </summary>
        /// <param name="error">The error to log.</param>
        public async Task<bool> LogAsync(Error error)
        {
            _ = error ?? throw new ArgumentNullException(nameof(error));

            // Track the GUID we made vs. what the store returns. If it's different, it's a dupe.
            var originalGuid = error.GUID;
            // if we're in a retry state, log directly to the queue
            if (QueuedInRetry(error, originalGuid))
            {
                return false;
            }
            try
            {
                bool result;
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    result = await LogErrorAsync(error).ConfigureAwait(false);
                }
                ProcessNotifications(error, originalGuid);
                return result;
            }
            catch (Exception ex)
            {
                _retryException = ex;
                // if we fail to write the error to the store, queue it for re-writing
                QueueError(error);
                return false;
            }
        }

        /// <summary>
        /// Deletes all non-protected errors from the log.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect.</param>
        public async Task<bool> ProtectAsync(Guid guid)
        {
            if (_isInRetry) return false; // no protecting allowed when failing, since we don't respect it in the queue anyway

            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
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
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
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
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
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
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
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
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
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
                foreach (var e in WriteQueue)
                {
                    if (e.GUID == guid)
                    {
                        return e;
                    }
                }
                return null;
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
            foreach (var err in WriteQueue)
            {
                if (e.ErrorHash == err.ErrorHash)
                {
                    e.GUID = err.GUID;
                    err.DuplicateCount++;
                    return;
                }
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
                if (!(await TestAsync().ConfigureAwait(false))) continue;

                // empty queue
                while (!WriteQueue.IsEmpty)
                {
                    // if we can't pop one off, get out of here
                    if (!WriteQueue.TryDequeue(out Error e)) return;

                    try
                    {
                        LogError(e);
                    }
                    catch
                    {
                        // if we had an error logging, stick it back in the queue and jump out, else we'll iterate this thing forever
                        QueueError(e);
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
                var error = new Error(new Exception("Test Exception"), Statics.Settings);
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

        internal static ErrorStore Get(ErrorStoreSettings settings)
        {
            if (settings?.Type == null) return new MemoryErrorStore();

            // a bit of validation
            if (settings.Size < 1)
                throw new ArgumentOutOfRangeException(nameof(settings), "ErrorStore 'size' must be at least 1");

            // Search by convention first
            // or...free for all!
            Type match = KnownStoreTypes.Find(s => s.Name == settings.Type + nameof(ErrorStore))
                      ?? KnownStoreTypes.Find(s => s.Name.Contains(settings.Type));

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

        /// <summary>
        /// The known list of error stores, loaded from all known DLLs in the running directory.
        /// </summary>
        public static List<Type> KnownStoreTypes { get; } = GetErrorStores();

        private static List<Type> GetErrorStores()
        {
            var result = new List<Type>();
            try
            {
                // Ensure all assemblies we expect are loaded before looking at types
                var assemblyUri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);
                var path = Path.GetDirectoryName(assemblyUri.LocalPath);
                foreach (var filename in Directory.GetFiles(path, "StackExchange.Exceptional.*.dll"))
                {
                    Assembly.LoadFrom(filename);
                }

                // Check for any implementers of ErrorStore anywhere
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // Try each assembly by itself, a load of exotic ones shouldn't fail us here
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (type.IsSubclassOf(typeof(ErrorStore)))
                            {
                                result.Add(type);
                            }
                        }
                    }
                    catch { /* nope */ }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    Trace.WriteLine("Error loading error stores: " + le.Message);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error loading error stores: " + ex.Message);
            }
            return result;
        }
    }
}
