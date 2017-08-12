using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.Serialization;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        /// <summary>
        /// The ErrorStore section of the configuration, optional and will default to a <see cref="Stores.MemoryErrorStore"/> if not specified.
        /// </summary>
        public ErrorStoreSettings ErrorStore { get; set; } = new ErrorStoreSettings();

        /// <summary>
        /// A settings object describing an error store.
        /// </summary>
        public class ErrorStoreSettings
        {
            /// <summary>
            /// The type of error store to use, File, SQL, Memory, etc.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public string Type { get; set; }

            /// <summary>
            /// The path to use on file based error stores.
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// The connection string to use on database based error stores.  If provided, ConnectionStringName is ignored.
            /// </summary>
            public string ConnectionString { get; set; }

            /// <summary>
            /// The name of the connection string to use from the application's configuration.
            /// </summary>
            public string ConnectionStringName { get; set; }

            /// <summary>
            /// The size of this error log, either how many to keep or how many to display depending on type.
            /// </summary>
            public int Size { get; set; } = 200;

            /// <summary>
            /// The duration in seconds of error groups to roll-up, similar errors within this timespan will be shown as duplicates.
            /// </summary>
            public int RollupSeconds { get; set; } = 600;

            /// <summary>
            /// The size of the backup queue to use for the log, after roll-ups, it's how many entries in memory can be stored before culling the oldest.
            /// </summary>
            public int BackupQueueSize { get; set; } = 1000;

            public void Populate(Settings settings)
            {
                var storeSettings = settings.Store;
                storeSettings.Type = Type;
                storeSettings.Path = Path;
                storeSettings.ConnectionString = ConnectionString;
#if !NETSTANDARD2_0
            storeSettings.ConnectionStringName = ErrorStore.ConnectionStringName;
#endif
                storeSettings.Size = Size;
                storeSettings.RollupPeriod = TimeSpan.FromSeconds(RollupSeconds);
                storeSettings.BackupQueueSize = BackupQueueSize;
            }
        }
    }
}