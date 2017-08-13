using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.Serialization;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        public ErrorStoreSettings ErrorStore { get; set; } 

        public class ErrorStoreSettings
        {
            public string Type { get; set; }

            public string Path { get; set; }

            public string ConnectionString { get; set; }

            public string ConnectionStringName { get; set; }

            public int? Size { get; set; } 

            public int? RollupSeconds { get; set; } 

            public int? BackupQueueSize { get; set; } 

            internal void Populate(Settings settings)
            {
                var storeSettings = settings.Store;
                storeSettings.Type = Type ?? storeSettings.Type;
                storeSettings.Path = Path ?? storeSettings.Path;
                storeSettings.ConnectionString = ConnectionString ?? storeSettings.ConnectionString;
#if !NETSTANDARD2_0
            storeSettings.ConnectionStringName = ConnectionStringName ?? storeSettings.ConnectionStringName;
#endif
                storeSettings.Size = Size ?? storeSettings.Size;
                if (RollupSeconds != null)
                    storeSettings.RollupPeriod = TimeSpan.FromSeconds(RollupSeconds.Value);
                storeSettings.BackupQueueSize = BackupQueueSize ?? storeSettings.BackupQueueSize;
            }
        }
    }
}