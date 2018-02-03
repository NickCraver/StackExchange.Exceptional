using System;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// A settings object got setting up an error store.
    /// </summary>
    public class ErrorStoreSettings
    {
        /// <summary>
        /// Application name to log with.
        /// </summary>
        public string ApplicationName { get; set; } = "My Application";

        private string _type;
        /// <summary>
        /// The type of error store to use, File, SQL, Memory, etc.
        /// </summary>
        public string Type
        {
            get => _type;
            set
            {
                if (value != _type)
                {
                    _type = value;
                    PropertyChanged?.Invoke(this, nameof(Type));
                }
            }
        }

        private string _path;
        /// <summary>
        /// For file-based error stores.
        /// The path to use on for file storage.
        /// </summary>
        public string Path
        {
            get => _path;
            set
            {
                if (value != _path)
                {
                    _path = value;
                    PropertyChanged?.Invoke(this, nameof(Path));
                }
            }
        }

        private string _connectionString;
        /// <summary>
        /// For database-based error stores.
        /// The connection string to use.  If provided, ConnectionStringName is ignored.
        /// </summary>
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                if (value != _connectionString)
                {
                    _connectionString = value;
                    PropertyChanged?.Invoke(this, nameof(ConnectionString));
                }
            }
        }

        private string _tableName;
        /// <summary>
        /// For database-based error stores.
        /// The table name (optionally including schema), e.g. "dbo.Exceptions" or "mySchema.MyExceptions" to use when storing exceptions. If null, the store default will be used.
        /// </summary>
        public string TableName
        {
            get => _tableName;
            set
            {
                if (value != _tableName)
                {
                    _tableName = value;
                    PropertyChanged?.Invoke(this, nameof(TableName));
                }
            }
        }

        private int _size = 200;
        /// <summary>
        /// The size of this error log, either how many to keep or how many to display depending on type.
        /// Defaults to 200.
        /// </summary>
        public int Size
        {
            get { return _size; }
            set
            {
                if (value != _size)
                {
                    _size = value;
                    PropertyChanged?.Invoke(this, nameof(Size));
                }
            }
        }

        private TimeSpan? _rollupPeriod = TimeSpan.FromMinutes(10);
        /// <summary>
        /// The duration of error groups to roll-up, similar errors within this timespan (those with the same stack trace) will be shown as duplicates.
        /// Defaults to 10 minutes.
        /// </summary>
        public TimeSpan? RollupPeriod
        {
            get => _rollupPeriod;
            set
            {
                if (value != _rollupPeriod)
                {
                    _rollupPeriod = value;
                    PropertyChanged?.Invoke(this, nameof(RollupPeriod));
                }
            }
        }

        private int _backupQueueSize = 1000;
        /// <summary>
        /// The size of the backup queue to use for the log, after roll-ups, it's how many entries in memory can be stored before culling the oldest.
        /// Defaults to 1000.
        /// </summary>
        public int BackupQueueSize
        {
            get => _backupQueueSize;
            set
            {
                if (value != _backupQueueSize)
                {
                    _backupQueueSize = value;
                    PropertyChanged?.Invoke(this, nameof(BackupQueueSize));
                }
            }
        }

        private TimeSpan _backupQueueRetryInterval = TimeSpan.FromSeconds(2);
        /// <summary>
        /// When a connection to the error store failed, how often to retry logging the errors in queue for logging.
        /// </summary>
        public TimeSpan BackupQueueRetryInterval
        {
            get => _backupQueueRetryInterval;
            set
            {
                if (value != _backupQueueRetryInterval)
                {
                    _backupQueueRetryInterval = value;
                    PropertyChanged?.Invoke(this, nameof(BackupQueueRetryInterval));
                }
            }
        }

        /// <summary>
        /// Fired when properties on this change, causing a need to reload the default queue.
        /// </summary>
        internal EventHandler<string> PropertyChanged;
    }
}
