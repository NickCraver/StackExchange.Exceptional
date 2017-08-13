using StackExchange.Exceptional.Internal;
using System;
using System.Configuration;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        [ConfigurationProperty("ErrorStore")]
        public ErrorStoreSettings ErrorStore => this["ErrorStore"] as ErrorStoreSettings;
        
        public class ErrorStoreSettings : ExceptionalElement
        {
            [ConfigurationProperty("type", IsRequired = true)]
            public string Type => Get("type");
            [ConfigurationProperty("path")]
            public string Path => Get("path");
            [ConfigurationProperty("connectionString")]
            public string ConnectionString => Get("connectionString");
            [ConfigurationProperty("connectionStringName")]
            public string ConnectionStringName => Get("connectionStringName");
            [ConfigurationProperty("size")]
            public int? Size => GetInt("size");
            [ConfigurationProperty("rollupSeconds")]
            public int? RollupSeconds => GetInt("rollupSeconds");
            [ConfigurationProperty("backupQueueSize")]
            public int? BackupQueueSize => GetInt("backupQueueSize");
            
            internal void Populate(Settings settings)
            {
                var s = settings.Store;
                s.Type = Type;
                if (Path.HasValue()) s.Path = Path;
                if (ConnectionString.HasValue()) s.ConnectionString = ConnectionString;
                if (ConnectionStringName.HasValue()) s.ConnectionStringName = ConnectionStringName;
                if (Size.HasValue) s.Size = Size.Value;
                if (RollupSeconds.HasValue) s.RollupPeriod = TimeSpan.FromSeconds(RollupSeconds.Value);
                if (BackupQueueSize.HasValue) s.BackupQueueSize = BackupQueueSize.Value;
            }
        }
    }
}