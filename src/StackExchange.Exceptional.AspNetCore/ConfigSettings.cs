using Microsoft.Extensions.Configuration;
using StackExchange.Exceptional.Internal;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// The Settings element for Exceptional's configuration.
    /// This is the legacy web.config settings, that only serve as an adapter to populate <see cref="Settings"/>.
    /// </summary>
    internal partial class ConfigSettings 
    {
        private IConfiguration _current;
        private readonly IConfigurationRoot _config;

        public ConfigSettings(IConfigurationRoot config)
        {
            this._config = config;
        }

        /// <summary>
        /// Trigger deserialization, which loads settings from the .config file.
        /// </summary>
        public void LoadSettings()
        {
            if (_current == null)
            {
                _current = _config.GetSection("Exceptional");
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