using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel;
using System.Configuration;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        public ErrorStoreSettings ErrorStore => new ErrorStoreSettings(_current.GetSection("ErrorStore"));

        public class ErrorStoreSettings 
        {
            private readonly IConfiguration _current;

            public string Type => _current["type"] as string;

            public string Path => _current["path"] as string;

            public string ConnectionString => _current["connectionString"] as string;
            public string ConnectionStringName => _current["connectionStringName"] as string;

            public int Size => Convert.ToInt32(_current["size"]);

            public int RollupSeconds => Convert.ToInt32(_current["rollupSeconds"]);

            public int BackupQueueSize => Convert.ToInt32(_current["backupQueueSize"]);

            public ErrorStoreSettings(IConfiguration current)
            {
                _current = current;
            }
        }
    }
}