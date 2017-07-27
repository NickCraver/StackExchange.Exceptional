using Microsoft.Extensions.Configuration;
using System.IO;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// The Settings element for Exceptional's configuration.
    /// This is the legacy web.config settings, that only serve as an adapter to populate <see cref="Settings"/>.
    /// </summary>
    internal partial class ConfigSettings 
    {
        private static IConfiguration _current;
        private readonly IConfigurationRoot _config;

        public ConfigSettings(IConfigurationRoot config)
        {
            _config = config;
        }

        /// <summary>
        /// Trigger deserialization, which loads settings from the .config file.
        /// </summary>
        public static void LoadSettings()
        {
            if (_current == null)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");
                _current = builder.Build().GetSection("Exceptional");
            }
        }

        /// <summary>
        /// Application name to log with.
        /// </summary>
        public string ApplicationName => _current["applicationName"] as string;

        /// <summary>
        /// The regular expression pattern of data keys to include. 
        /// For example, "Redis.*" would include all keys that start with Redis.
        /// </summary>
        public string DataIncludePattern => _current["dataIncludePattern"] as string;
    }
}