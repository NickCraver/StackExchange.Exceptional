using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using StackExchange.Exceptional.Stores;
using StackExchange.Exceptional.Extensions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Represents an error log capable of storing and retrieving errors generated in an ASP.NET Web application.
    /// </summary>
    public abstract partial class ErrorStore
    {
        private static ErrorStore _defaultStore;
        
        [ThreadStatic]
        private static List<Regex> _ignoreRegex;

        [ThreadStatic]
        private static List<string> _ignoreExceptions;

        private static ConcurrentQueue<Error> _writeQueue;

        private static bool _enableLogging = true;
        private static Thread _retryThread;
        private static readonly object _retryLock = new object();
        // TODO: possibly make this configurable
        internal const int _retryDelayMiliseconds = 2000;
        private static bool _isInRetry;
        private static Exception _retryException;
        internal const string CustomDataErrorKey = "CustomDataFetchError";

        /// <summary>
        /// The default number of exceptions (rollups count as 1) to buffer in memory in the event of an error store outage
        /// </summary>
        public const int DefaultBackupQueueSize = 1000;
        /// <summary>
        /// The default number of seconds to roll up errors for.  Identical stack trace errors within 10 minutes get a DuplicateCount++ instead of a separate exception logged.
        /// </summary>
        public const int DefaultRollupSeconds = 600;
        
        /// <summary>
        /// Base constructor of the error store to set common properties
        /// </summary>
        protected ErrorStore(ErrorStoreSettings settings) : this(settings.RollupSeconds, settings.BackupQueueSize) { }

        /// <summary>
        /// Creates an error store with the specified rollup
        /// </summary>
        protected ErrorStore(int rollupSeconds, int backupQueueSize = DefaultBackupQueueSize)
        {
            if (rollupSeconds > 0)
                RollupThreshold = TimeSpan.FromSeconds(rollupSeconds);

            if (backupQueueSize > 0)
                BackupQueueSize = backupQueueSize;
            else
                BackupQueueSize = DefaultBackupQueueSize;
        }

        /// <summary>
        /// The size of the backup/retry queue for logging, defaults to 1000
        /// </summary>
        public int BackupQueueSize { get; set; }

        /// <summary>
        /// Gets if this error store is 
        /// </summary>
        public bool InFailureMode { get { return _isInRetry; } }
        
        /// <summary>
        /// The Rollup threshold within which errors logged rapidly are rolled up
        /// </summary>
        protected TimeSpan? RollupThreshold;

        /// <summary>
        /// The last time this error store failed to write an error
        /// </summary>
        protected DateTime? LastWriteFailure;

        /// <summary>
        /// Logs an error in log for the application
        /// </summary>
        protected abstract void LogError(Error error);

        /// <summary>
        /// Retrieves a single error based on Id
        /// </summary>
        protected abstract Error GetError(Guid guid);

        /// <summary>
        /// Prevents error identfied by 'id' from being deleted when the error log is full, if the store supports it
        /// </summary>
        protected abstract bool ProtectError(Guid guid);

        /// <summary>
        /// Deletes a specific error from the log
        /// </summary>
        protected abstract bool DeleteError(Guid guid);

        /// <summary>
        /// Deletes a specific error from the log, any traces of it
        /// </summary>
        protected virtual bool HardDeleteError(Guid guid) { return DeleteError(guid); }

        /// <summary>
        /// Deletes all non-protected errors from the log
        /// </summary>
        protected abstract bool DeleteAllErrors();

        /// <summary>
        /// Retrieves all of the errors in the log
        /// </summary>
        protected abstract int GetAllErrors(List<Error> list);

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if null
        /// </summary>
        protected abstract int GetErrorCount(DateTime? since = null);

        /// <summary>
        /// Get the name of this error log store implementation.
        /// </summary>
        public virtual string Name { get { return GetType().Name; } }

        /// <summary>
        /// Get the name of this error log store implementation.
        /// </summary>
        public abstract ErrorStoreType Type { get; }

        private static string _applicationName { get; set; }
        /// <summary>
        /// Gets the name of the application to which the log is scoped.
        /// </summary>
        public static string ApplicationName
        {
            get { return _applicationName ?? (_applicationName = Settings.Current.ApplicationName); }
        }

        /// <summary>
        /// Gets the name of the machine logging these errors.
        /// </summary>
        public virtual string MachineName
        {
            get { return Environment.MachineName; }
        }

        /// <summary>
        /// Gets the list of exceptions to ignore specified in the configuration file
        /// </summary>
        public static List<Regex> IgnoreRegexes
        {
            get { return _ignoreRegex ?? (_ignoreRegex = Settings.Current.Ignore.Regexes.All.Select(r => r.PatternRegex).ToList()); }
        }

        /// <summary>
        /// Gets the list of exceptions to ignore specified in the configuration file
        /// 
        /// </summary>
        public static List<string> IgnoreExceptions
        {
            get { return _ignoreExceptions ?? (_ignoreExceptions = Settings.Current.Ignore.Types.All.Select(r => r.Type).ToList()); }
        }

        /// <summary>
        /// Gets the default error store specified in the configuration, 
        /// or the in-memory store if none is configured.
        /// </summary>
        public static ErrorStore Default
        {
            get { return _defaultStore ?? (_defaultStore = GetErrorStoreFromConfig()); }
        }
        
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
        public static ConcurrentQueue<Error> WriteQueue
        {
            get { return _writeQueue ?? (_writeQueue = new ConcurrentQueue<Error>()); }
        }

        /// <summary>
        /// Gets the last exception that happened when trying to log exceptions
        /// </summary>
        public static Exception LastRetryException
        {
            get { return _retryException; }
        }

        /// <summary>
        /// Logs an error in log for the application
        /// </summary>
        public void Log(Error error)
        {
            if (error == null) throw new ArgumentNullException("error");

            // if we're in a retry state, log directly to the queue
            if (_isInRetry)
            {
                QueueError(error);
                return;
            }
            try
            {
                LogError(error);
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
        public bool Protect(Guid guid)
        {
            if (_isInRetry) return false; // no protecting allowed when failing, since we don't respect it in the queue anyway

            return ProtectError(guid);
        }

        /// <summary>
        /// Deletes an error from the log with the specified id
        /// </summary>
        public bool Delete(Guid guid)
        {
            if (_isInRetry) return false; // no deleting from the retry queue

            try { return DeleteError(guid); }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes all non-protected errors from the log
        /// </summary>
        public bool DeleteAll()
        {
            if (_isInRetry)
            {
                _writeQueue = new ConcurrentQueue<Error>();
                return true;
            }

            try { return DeleteAllErrors(); }
            catch (Exception ex)
            {
                BeginRetry(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a specific exception with the specified guid
        /// </summary>
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
        public int GetAll(List<Error> errors)
        {
            if (_isInRetry)
            {
                errors.AddRange(WriteQueue);
                return errors.Count;
            }

            try { return GetAllErrors(errors); }
            catch (Exception ex) { BeginRetry(ex); }
            return 0;
        }

        /// <summary>
        /// Gets the count of exceptions, optionally those since a certain date
        /// </summary>
        public int GetCount(DateTime? since = null)
        {
            if (_isInRetry)
            {
                return WriteQueue.Count;
            }

            try { return GetErrorCount(since); }
            catch (Exception ex) { BeginRetry(ex); }
            return 0;
        }

        /// <summary>
        /// Queues an error into the backup/retry queue
        /// </summary>
        /// <remarks>These will be written to the store when we're able to connect again</remarks>
        public void QueueError(Error e)
        {
            // try and rollup in the queue, to save space
            foreach (var err in WriteQueue.Where(err => e.ErrorHash == err.ErrorHash))
            {
                err.DuplicateCount++;
                return;
            }

            // only queue if we're under the cap
            if (WriteQueue.Count < BackupQueueSize)
                WriteQueue.Enqueue(e);

            // spin up the retry mechanism
            BeginRetry();
        }

        private static void BeginRetry(Exception ex = null)
        {
            lock (_retryLock)
            {
                if (ex != null) _retryException = ex;
                _isInRetry = true;

                // are we already spun up?
                if (_retryThread != null && _retryThread.IsAlive) return;

                _retryThread = new Thread(TryFlushQueue);
                _retryThread.Start();
            }
        }

        private static void TryFlushQueue()
        {
            if (!_isInRetry && WriteQueue.IsEmpty) return;

            while (true)
            {
                Thread.Sleep(_retryDelayMiliseconds);

                // if the error store is still down, sleep again
                if (!Default.Test()) continue;

                // empty queue
                while (!WriteQueue.IsEmpty)
                {
                    Error e;
                    // if we can't pop one off, get out of here
                    if (!WriteQueue.TryDequeue(out e)) return;

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
            return GetFromSettings(Settings.Current.ErrorStore) ?? new MemoryErrorStore();
        }

        private static ErrorStore GetFromSettings(ErrorStoreSettings settings)
        {
            if (settings == null) return null;

            // a bit of validation
            if (settings.Type.IsNullOrEmpty())
                throw new ArgumentOutOfRangeException("settings", "ErrorStore 'type' must be specified");
            if (settings.Size < 1) 
                throw new ArgumentOutOfRangeException("settings","ErrorStore 'size' must be positive");

            switch (settings.Type)
            {
                case "JSON":
                    return new JSONErrorStore(settings);
                case "Memory":
                    return new MemoryErrorStore(settings);
                case "SQL":
                    return new SQLErrorStore(settings);
                default:
                    throw new Exception("Unknwon error store type: " + settings.Type);
            }
        }

        /// <summary>
        /// For logging an exception with no HttpContext, most commonly used in non-web applications 
        /// so that they don't have to carry a reference to System.Web
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="appendFullStackTrace">Wehther to append a full stack trace to the exception's detail</param>
        /// <param name="rollupPerServer">Whether to log up per-server, e.g. errors are only duplicates if they have same stack on the same machine</param>
        /// <param name="customData">Any custom data to store with the exception like UserId, etc...this will be rendered as JSON in the error view for script use</param>
        public static void LogExceptionWithoutContext(Exception ex, bool appendFullStackTrace = false, bool rollupPerServer = false, Dictionary<string, string> customData = null)
        {
            LogException(ex, null, appendFullStackTrace, rollupPerServer, customData);
        }

        /// <summary>
        /// Logs an exception to the configured error store, or the in-memory default store if none is configured
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="context">The HTTPContext to record variables from.  If this isn't a web request, pass <see langword="null" /> in here</param>
        /// <param name="appendFullStackTrace">Wehther to append a full stack trace to the exception's detail</param>
        /// <param name="rollupPerServer">Whether to log up per-server, e.g. errors are only duplicates if they have same stack on the same machine</param>
        /// <param name="customData">Any custom data to store with the exception like UserId, etc...this will be rendered as JSON in the error view for script use</param>
        /// <remarks>
        /// When dealing with a non web requests, pass <see langword="null" /> in for context.  
        /// It shouldn't be forgotten for most web application usages, so it's not an optional parameter.
        /// </remarks>
        public static void LogException(Exception ex, HttpContext context, bool appendFullStackTrace = false, bool rollupPerServer = false, Dictionary<string, string> customData = null)
        {
            if (!_enableLogging) return;
            try
            {
                if (IgnoreRegexes.Any(re => re.IsMatch(ex.ToString())))
                    return;
                if (IgnoreExceptions.Any(type => IsDescendentOf(ex.GetType(), type.ToString())))
                    return;

                if (customData == null && GetCustomData != null)
                {
                    customData = new Dictionary<string, string>();
                    try
                    {
                        GetCustomData(ex, context, customData);
                    }
                    catch (Exception cde)
                    {
                        // if there was an error getting custom errors, log it so we can display such in the view...and not fail to log the original error
                        customData.Add(CustomDataErrorKey, cde.ToString());
                    }
                }

                var error = new Error(ex, context)
                                {
                                    RollupPerServer = rollupPerServer,
                                    CustomData = customData
                                };

                if (ex != null && ex.Data.Contains("SQL"))
                    error.SQL = ex.Data["SQL"] as string;

                if (appendFullStackTrace)
                {
                    var frames = new StackTrace(fNeedFileInfo: true).GetFrames();
                    if (frames != null)
                        error.Detail += "\n\nFull Trace:\n\n" + string.Join("", frames.Skip(2));
                }

                Trace.WriteLine(ex); // always echo the error to trace for local debugging
                Default.Log(error);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        /// <summary>
        /// Returns true if t is of className, or descendent from className
        /// </summary>
        private static bool IsDescendentOf(Type t, string className)
        {
            if (t.FullName == className) return true;

            return t.BaseType != null && IsDescendentOf(t.BaseType, className);
        }
    }
}