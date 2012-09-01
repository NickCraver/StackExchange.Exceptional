using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

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

        public override string Name { get { return "Memory Error Store"; } }
        public override ErrorStoreType Type { get { return ErrorStoreType.Memory; } }

        protected override bool ProtectError(Guid guid)
        {
            return false; // NO QUARTER FOR THE WICKED - no seriously, it's not sane to do this for a volatile memory store.
        }

        protected override bool DeleteError(Guid guid)
        {
            lock(_lock)
            {
                return _errors.RemoveAll(e => e.GUID == guid) > 0;
            }
        }

        protected override bool DeleteAllErrors()
        {
            lock (_lock)
            {
                _errors.Clear();
            }
            return true;
        }

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
                        dupe.DuplicateCount+= error.DuplicateCount;
                        return;
                    }
                }

                if (_errors.Count >= _size)
                    _errors.RemoveAt(0);

                _errors.Add(error);
            }
        }

        protected override Error GetError(Guid guid)
        {
            lock (_lock)
            {
                return _errors == null ? null : _errors.FirstOrDefault(e => e.GUID == guid);
            }
        }

        protected override int GetAllErrors(List<Error> errors)
        {
            lock (_lock)
            {
                if (_errors == null) return 0;
                errors.AddRange(_errors.Select(e => e.Clone()));
                return _errors.Count;
            }
        }

        protected override int GetErrorCount(DateTime? since = null)
        {
            lock (_lock)
            {
                if (_errors == null) return 0;
                return !since.HasValue ? _errors.Count : _errors.Count(e => e.CreationDate >= since);
            }
        }
    }
}