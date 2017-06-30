using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using StackExchange.Exceptional.Email;
using StackExchange.Exceptional.Stores;
using StackExchange.Exceptional.Internal;

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
        /// Base constructor of the error store to set common properties
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>     
        protected ErrorStore(ErrorStoreSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Gets if this error store is 
        /// </summary>
        public bool InFailureMode => _isInRetry;

        /// <summary>
        /// The last time this error store failed to write an error
        /// </summary>
        protected DateTime? LastWriteFailure;

        /// <summary>
        /// Logs an error in log for the application
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected abstract void LogError(Error error);

        /// <summary>
        /// Retrieves a single error based on Id
        /// </summary>
        /// <param name="guid">The guid of the error to retrieve.</param>
        protected abstract Error GetError(Guid guid);

        /// <summary>
        /// Prevents error identfied by 'id' from being deleted when the error log is full, if the store supports it
        /// </summary>
        /// <param name="guid">The guid of the error to protect.</param>
        protected abstract bool ProtectError(Guid guid);

        /// <summary>
        /// Protects a list of errors in the log.
        /// </summary>
        /// <param name="guids">The guids of the errors to protect.</param>
        protected virtual bool ProtectErrors(IEnumerable<Guid> guids)
        {
            var success = true;
            foreach (var guid in guids)
            {
                if (!ProtectError(guid))
                {
                    success = false;
                }
            }
            return success;
        }

        /// <summary>
        /// Deletes a specific error from the log.
        /// </summary>
        /// <param name="guid">The guid of the error to delete.</param>
        protected abstract bool DeleteError(Guid guid);

        /// <summary>
        /// Deletes a list of errors from the log, only if they are not protected.
        /// </summary>
        /// <param name="guids">The guids of the errors to delete.</param>
        protected virtual bool DeleteErrors(IEnumerable<Guid> guids)
        {
            var success = true;
            foreach (var guid in guids)
            {
                if (!DeleteError(guid))
                {
                    success = false;
                }
            }
            return success;
        }

        /// <summary>
        /// Deletes a specific error from the log, any traces of it
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> ID of the error to hard delete.</param>
        protected virtual bool HardDeleteError(Guid guid) => DeleteError(guid);

        /// <summary>
        /// Deletes all non-protected errors from the log
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        protected abstract bool DeleteAllErrors(string applicationName = null);

        /// <summary>
        /// Retrieves all of the errors in the log
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        protected abstract List<Error> GetAllErrors(string applicationName = null);

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if null
        /// </summary>
        /// <param name="since">The date to get errors since.</param>
        /// <param name="applicationName">The application name to get an error count for.</param>
        protected abstract int GetErrorCount(DateTime? since = null, string applicationName = null);

        /// <summary>
        /// Get the name of this error log store implementation.
        /// </summary>
        public virtual string Name => GetType().Name;

        private static string _applicationName { get; set; }
        /// <summary>
        /// Gets the name of the application to which the log is scoped.
        /// </summary>
        public static string ApplicationName => _applicationName ?? (_applicationName = ExceptionalSettings.Current.ApplicationName);

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
        /// Gets the write queue for errors, which is populated in the case of a write failure
        /// </summary>
        public static ConcurrentQueue<Error> WriteQueue => _writeQueue ?? (_writeQueue = new ConcurrentQueue<Error>());

        /// <summary>
        /// Gets the last exception that happened when trying to log exceptions
        /// </summary>
        public Exception LastRetryException => _retryException;

        /// <summary>
        /// Logs an error in log for the application
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
                ErrorEmailer.SendMail(error);
                return;
            }
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    LogError(error);
                }
                if (originalGuid != error.GUID) error.IsDuplicate = true;
                ErrorEmailer.SendMail(error);
            }
            catch (Exception ex)
            {
                _retryException = ex;
                // if we fail to write the error to the store, queue it for re-writing
                QueueError(error);
            }
        }

        /// <summary>
        /// Deletes all non-protected errors from the log
        /// </summary>
        /// <param name="guid">The guid of the error to protect.</param>
        public bool Protect(Guid guid)
        {
            if (_isInRetry) return false; // no protecting allowed when failing, since we don't respect it in the queue anyway

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                return ProtectError(guid);
            }
        }

        /// <summary>
        /// Protects a list of errors in the log
        /// </summary>
        /// <param name="guids">The guids of the errors to protect.</param>
        public bool ProtectList(IEnumerable<Guid> guids)
        {
            if (_isInRetry) return false; // no protecting allowed when failing, since we don't respect it in the queue anyway

            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    return ProtectErrors(guids);
                }
            }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes an error from the log with the specified id
        /// </summary>
        /// <param name="guid">The guid of the error to delete.</param>
        public bool Delete(Guid guid)
        {
            if (_isInRetry) return false; // no deleting from the retry queue

            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    return DeleteError(guid);
                }
            }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes a list of non-protected errors from the log
        /// </summary>
        /// <param name="guids">The guids of the errors to delete.</param>
        public bool DeleteList(IEnumerable<Guid> guids)
        {
            if (_isInRetry) return false; // no deleting from the retry queue

            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    return DeleteErrors(guids);
                }
            }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes all non-protected errors from the log
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        public bool DeleteAll(string applicationName = null)
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
                    return DeleteAllErrors(applicationName);
                }
            }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a specific exception with the specified guid
        /// </summary>
        /// <param name="guid">The guid of the error to retrieve.</param>
        public Error Get(Guid guid)
        {
            if (_isInRetry)
            {
                return WriteQueue.FirstOrDefault(e => e.GUID == guid);
            }

            try { return GetError(guid); }
            catch (Exception ex) { BeginRetry(ex); }
            return null;
        }

        /// <summary>
        /// Gets all in the store, including those in the backup queue if it's in use
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        public List<Error> GetAll(string applicationName = null)
        {
            if (_isInRetry)
            {
                // Dupe it!
                return new List<Error>(WriteQueue);
            }

            try { return GetAllErrors(applicationName); }
            catch (Exception ex) { BeginRetry(ex); }
            return new List<Error>();
        }

        /// <summary>
        /// Gets the count of exceptions, optionally those since a certain date
        /// </summary>
        /// <param name="since">The minimum date to fetch errors after.</param>
        /// <param name="applicationName">The application name to fetch errors for.</param>
        public int GetCount(DateTime? since = null, string applicationName = null)
        {
            if (_isInRetry)
            {
                return WriteQueue.Count;
            }

            try { return GetErrorCount(since, applicationName); }
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
            // try and rollup in the queue, to save space
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

        private void TryFlushQueue()
        {
            if (!_isInRetry && WriteQueue.IsEmpty) return;

            while (true)
            {
                Thread.Sleep(Settings.BackupQueueRetryInterval);

                // if the error store is still down, sleep again
                if (!Default.Test()) continue;

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
        /// Tests to see if this error store is working
        /// </summary>
        public bool Test()
        {
            try
            {
                var error = new Error(new Exception("Test Exception"));
                LogError(error);
                HardDeleteError(error.GUID);
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
            return GetFromSettings(ExceptionalSettings.Current.Store) ?? new MemoryErrorStore();
        }

        private static ErrorStore GetFromSettings(ErrorStoreSettings settings)
        {
            if (settings == null) return null;

            // a bit of validation
            if (settings.Type.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException(nameof(settings), "ErrorStore 'type' must be specified");
            if (settings.Size < 1)
                throw new ArgumentOutOfRangeException(nameof(settings),"ErrorStore 'size' must be positive");

            var storeTypes = GetErrorStores();
            // Search by convention first
            // or...free for all!
            Type match = storeTypes.Find(s => s.Name == settings.Type + "ErrorStore") ?? storeTypes.Find(s => s.Name.Contains(settings.Type));

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
            var result = new List<Type>();
            // Get the current directory, based on Where StackExchange.Exceptional.dll is located

            var assemblyUri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);
            var dir = Path.GetDirectoryName(assemblyUri.LocalPath);

            if (String.IsNullOrEmpty(dir))
            {
                Trace.WriteLine("Error loading Error stores, abs path: " + assemblyUri.AbsolutePath);
                return result;
            }

            try
            {
                // It's intentional even the core error stores load this way, as a sanity check
                foreach (var filename in Directory.GetFiles(dir, "StackExchange.Exceptional*.dll"))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(filename);
                        result.AddRange(assembly.GetTypes().Where(type => type.IsSubclassOf(typeof (ErrorStore))));
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Error loading ErrorStore types from {filename}: {e.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error loading error stores: " + ex.Message);
            }
            return result;
        }

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
    }
}
