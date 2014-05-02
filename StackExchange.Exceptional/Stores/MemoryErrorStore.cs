using System.Collections.Generic;
using System.Linq;
using System;
using StackExchange.Exceptional.Extensions;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// An <see cref="ErrorStore"/> implementation that uses memory as its backing store. 
    /// </summary>
    public sealed class MemoryErrorStore : ErrorStore
    {
        // The concurrent collection that provides the storage
        private static List<Error> _errors;
        private static readonly object _lock = new object();
        private readonly int _size = DefaultSize;

        /// <summary>
        /// The maximum count of errors stored before the first is overwritten.
        /// </summary>
        public const int MaximumSize = 500;

        /// <summary>
        /// The default maximum count of errors stored before the first is overwritten.
        /// </summary>        
        public const int DefaultSize = 200;

        /// <summary>
        /// Creates a new instance of <see cref="MemoryErrorStore"/> with defaults.
        /// </summary>
        public MemoryErrorStore() : this(new ErrorStoreSettings()) { }

        /// <summary>
        /// Creates a new instance of <see cref="MemoryErrorStore"/> with the given size.
        /// </summary>
        public MemoryErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            _size = Math.Min(settings.Size, MaximumSize);
        }

        /// <summary>
        /// Creates a new instance of <see cref="MemoryErrorStore"/> with the given size.
        /// </summary>
        /// <param name="size">How many errors to limit the log to, the size+1th error (oldest) will be removed if exceeded</param>
        /// <param name="rollupSeconds">The rollup seconds, defaults to <see cref="ErrorStore.DefaultRollupSeconds"/>, duplicate errors within this time period will be rolled up</param>
        public MemoryErrorStore(int size = DefaultSize, int rollupSeconds = DefaultRollupSeconds) : base(rollupSeconds)
        {
            _size = Math.Min(size, MaximumSize);
        }

        /// <summary>
        /// Name for this error store
        /// </summary>
        public override string Name { get { return "Memory Error Store"; } }

        /// <summary>
        /// Protects an error from deletion, by setting IsProtected = true
        /// </summary>
        /// <param name="guid">The guid of the error to protect</param>
        /// <returns>True if the error was found and protected, false otherwise</returns>
        protected override bool ProtectError(Guid guid)
        {
            lock (_lock)
            {
                var error = _errors.FirstOrDefault(e => e.GUID == guid);
                if (error != null)
                {
                    error.IsProtected = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Deletes an error, by deleting it from the in-memory log
        /// </summary>
        /// <param name="guid">The guid of the error to delete</param>
        /// <returns>True if the error was found and deleted, false otherwise</returns>
        protected override bool DeleteError(Guid guid)
        {
            lock(_lock)
            {
                return _errors.RemoveAll(e => e.GUID == guid) > 0;
            }
        }
        
        /// <summary>
        /// Deleted all errors in the log, by clearing the in-memory log
        /// </summary>
        /// <returns>True in all cases</returns>
        protected override bool DeleteAllErrors(string applicationName = null)
        {
            lock (_lock)
            {
                if (applicationName.HasValue())
                    _errors.RemoveAll(e => !e.IsProtected && e.ApplicationName == applicationName);
                else
                    _errors.RemoveAll(e => !e.IsProtected);
            }
            return true;
        }

        /// <summary>
        /// Logs the error to the in-memory error log
        /// If the rollup conditions are met, then the matching error will have a DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new row for the error
        /// </summary>
        /// <param name="error">The error to log</param>
        protected override void LogError(Error error)
        {
            lock(_lock)
            {
                if (_errors == null)
                    _errors = new List<Error>(_size);

                if (RollupThreshold.HasValue && _errors.Count > 0)
                {
                    var minDate = DateTime.UtcNow.Add(RollupThreshold.Value.Negate());
                    var dupe = _errors.FirstOrDefault(e => e.ErrorHash == error.ErrorHash && e.CreationDate > minDate);
                    if (dupe != null)
                    {
                        dupe.DuplicateCount += error.DuplicateCount;
                        error.GUID = dupe.GUID;
                        return;
                    }
                }

                if (_errors.Count >= _size)
                {
                    _errors.Remove(_errors.FirstOrDefault(e => !e.IsProtected));
                }

                _errors.Add(error);
            }
        }

        /// <summary>
        /// Gets the error with the specified guid from the in-memory log
        /// </summary>
        /// <param name="guid">The guid of the error to retrieve</param>
        /// <returns>The error object if found, null otherwise</returns>
        protected override Error GetError(Guid guid)
        {
            lock (_lock)
            {
                return _errors == null ? null : _errors.FirstOrDefault(e => e.GUID == guid);
            }
        }

        /// <summary>
        /// Retrieves all of the errors in the log
        /// </summary>
        protected override int GetAllErrors(List<Error> errors, string applicationName = null)
        {
            lock (_lock)
            {
                if (_errors == null) return 0;

                IEnumerable<Error> result = _errors;
                if (applicationName.HasValue())
                {
                    result = result.Where(e => e.ApplicationName == applicationName);
                }

                errors.AddRange(result.Select(e => e.Clone()));
                return _errors.Count;
            }
        }

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if null
        /// </summary>
        protected override int GetErrorCount(DateTime? since = null, string applicationName = null)
        {
            lock (_lock)
            {
                if (_errors == null) return 0;
                if (applicationName.HasValue())
                {
                    return !since.HasValue
                        ? _errors.Count(e => e.ApplicationName == applicationName)
                        : _errors.Count(e => e.CreationDate >= since && e.ApplicationName == applicationName);
                }

                return !since.HasValue ? _errors.Count : _errors.Count(e => e.CreationDate >= since);

            }
        }
    }
}