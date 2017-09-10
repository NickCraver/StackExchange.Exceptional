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

        /// <summary>
        /// The type of error store to use, File, SQL, Memory, etc.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// For file-based error stores.
        /// The path to use on for file storage.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// For database-based error stores.
        /// The connection string to use.  If provided, ConnectionStringName is ignored.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The size of this error log, either how many to keep or how many to display depending on type.
        /// Defaults to 200.
        /// </summary>
        public int Size { get; set; } = 200;

        /// <summary>
        /// The duration of error groups to roll-up, similar errors within this timespan (those with the same stack trace) will be shown as duplicates.
        /// Defaults to 10 minutes.
        /// </summary>
        public TimeSpan? RollupPeriod { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// The size of the backup queue to use for the log, after roll-ups, it's how many entries in memory can be stored before culling the oldest.
        /// Defaults to 1000.
        /// </summary>
        public int BackupQueueSize { get; set; } = 1000;

        /// <summary>
        /// When a connection to the error store failed, how often to retry logging the errors in queue for logging.
        /// </summary>
        public TimeSpan BackupQueueRetryInterval { get; set; } = TimeSpan.FromSeconds(2);
    }
}