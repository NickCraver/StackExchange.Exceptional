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
            [JsonProperty(Required = Required.Always)]
            public string Type { get; set; }

            public string Path { get; set; }

            public string ConnectionString { get; set; }

            public string ConnectionStringName { get; set; }

            public int Size { get; set; } 

            public int RollupSeconds { get; set; } 

            public int BackupQueueSize { get; set; } 

            internal void Populate(Settings settings)
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