using System.Collections.Generic;
using System.Linq;
using System;
using StackExchange.Exceptional.Internal;
using System.Threading.Tasks;

namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// An <see cref="ErrorStore"/> implementation that uses memory as its backing store. 
    /// </summary>
    public sealed class MemoryErrorStore : ErrorStore
    {
        // The concurrent collection that provides the storage
        private List<Error> _errors;
        private readonly object _lock = new object();
        private readonly int _size = DefaultSize;

        /// <summary>
        /// The maximum count of errors stored before the first is overwritten.
        /// </summary>
        public const int MaximumSize = 1000;

        /// <summary>
        /// The default maximum count of errors stored before the first is overwritten.
        /// </summary>        
        public const int DefaultSize = 250;

        /// <summary>
        /// Creates a new instance of <see cref="MemoryErrorStore"/> with defaults.
        /// </summary>
        public MemoryErrorStore() : this(new ErrorStoreSettings()) { }

        /// <summary>
        /// Creates a new instance of <see cref="MemoryErrorStore"/> with the given size.
        /// </summary>
        /// <param name="settings">The <see cref="ErrorStoreSettings"/> for this store.</param>  
        public MemoryErrorStore(ErrorStoreSettings settings) : base(settings)
        {
            _size = Math.Min(settings.Size, MaximumSize);
        }

        /// <summary>
        /// Creates a new instance of <see cref="MemoryErrorStore"/> with the given size.
        /// </summary>
        /// <param name="size">How many errors to limit the log to, the size+1th error (oldest) will be removed if exceeded.</param>
        public MemoryErrorStore(int size = DefaultSize)
            : base(new ErrorStoreSettings()
            {
                Size = size
            })
        {
            _size = Math.Min(size, MaximumSize);
        }

        /// <summary>
        /// Name for this error store
        /// </summary>
        public override string Name => "Memory Error Store";

        /// <summary>
        /// Protects an error from deletion, by setting IsProtected = <c>true</c>.
        /// </summary>
        /// <param name="guid">The GUID of the error to protect.</param>
        /// <returns><c>true</c> if the error was found and protected, <c>false</c> otherwise.</returns>
        protected override Task<bool> ProtectErrorAsync(Guid guid)
        {
            lock (_lock)
            {
                var error = _errors.Find(e => e.GUID == guid);
                if (error != null)
                {
                    error.IsProtected = true;
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }

        /// <summary>
        /// Deletes an error, by deleting it from the in-memory log.
        /// </summary>
        /// <param name="guid">The GUID of the error to delete.</param>
        /// <returns><c>true</c> if the error was found and deleted, <c>false</c> otherwise.</returns>
        protected override Task<bool> DeleteErrorAsync(Guid guid)
        {
            lock(_lock)
            {
                return Task.FromResult(_errors.RemoveAll(e => e.GUID == guid) > 0);
            }
        }

        /// <summary>
        /// Deleted all errors in the log, by clearing the in-memory log.
        /// </summary>
        /// <param name="applicationName">The name of the application to delete all errors for.</param>
        /// <returns><c>true</c> in all cases.</returns>
        protected override Task<bool> DeleteAllErrorsAsync(string applicationName = null)
        {
            lock (_lock)
            {
                if (applicationName.HasValue())
                    _errors.RemoveAll(e => !e.IsProtected && e.ApplicationName == applicationName);
                else
                    _errors.RemoveAll(e => !e.IsProtected);
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Logs the error to the in-memory error log.
        /// If the roll-up conditions are met, then the matching error will have a 
        /// DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new entry for the error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        protected override bool LogError(Error error)
        {
            lock (_lock)
            {
                if (_errors == null)
                    _errors = new List<Error>(_size);

                if (Settings.RollupPeriod.HasValue && _errors.Count > 0)
                {
                    var minDate = DateTime.UtcNow.Subtract(Settings.RollupPeriod.Value);
                    var dupe = _errors.Find(e => e.ErrorHash == error.ErrorHash && e.CreationDate > minDate);
                    if (dupe != null)
                    {
                        dupe.DuplicateCount += error.DuplicateCount;
                        if (dupe.LastLogDate == null || error.CreationDate > dupe.LastLogDate)
                        {
                            dupe.LastLogDate = error.CreationDate;
                        }
                        error.GUID = dupe.GUID;
                        return true;
                    }
                }

                if (_errors.Count >= _size)
                {
                    _errors.Remove(_errors.Find(e => !e.IsProtected));
                }

                _errors.Add(error);
            }
            return true;
        }

        /// <summary>
        /// Gets the error with the specified GUID from the in-memory log.
        /// </summary>
        /// <param name="guid">The GIUID of the error to retrieve.</param>
        /// <returns>The error object if found, <c>null</c> otherwise.</returns>
        protected override Task<Error> GetErrorAsync(Guid guid)
        {
            lock (_lock)
            {
                return Task.FromResult(_errors?.FirstOrDefault(e => e.GUID == guid));
            }
        }

        /// <summary>
        /// Retrieves all of the errors in the log.
        /// </summary>
        /// <param name="applicationName">The name of the application to get all errors for.</param>
        protected override Task<List<Error>> GetAllErrorsAsync(string applicationName = null)
        {
            lock (_lock)
            {
                if (_errors == null) return Task.FromResult(new List<Error>());

                IEnumerable<Error> result = _errors;
                if (applicationName.HasValue())
                {
                    result = result.Where(e => e.ApplicationName == applicationName);
                }

                return Task.FromResult(result.Select(e => e.Clone()).ToList());
            }
        }

        /// <summary>
        /// Retrieves a count of application errors since the specified date, or all time if <c>null</c>.
        /// </summary>
        /// <param name="since">The date to get errors since.</param>
        /// <param name="applicationName">The application name to get an error count for.</param>
        protected override Task<int> GetErrorCountAsync(DateTime? since = null, string applicationName = null)
        {
            lock (_lock)
            {
                if (_errors == null) return Task.FromResult(0);
                if (applicationName.HasValue())
                {
                    return Task.FromResult(!since.HasValue
                        ? _errors.Count(e => e.ApplicationName == applicationName)
                        : _errors.Count(e => e.CreationDate >= since && e.ApplicationName == applicationName));
                }

                return Task.FromResult(!since.HasValue ? _errors.Count : _errors.Count(e => e.CreationDate >= since));
            }
        }
    }
}
