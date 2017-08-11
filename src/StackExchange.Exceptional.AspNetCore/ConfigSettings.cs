using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Notifiers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// The Settings element for Exceptional's configuration.
    /// This is the legacy web.config settings, that only serve as an adapter to populate <see cref="Settings"/>.
    /// </summary>
    public partial class ConfigSettings
    {
        const string CONFIGSECTION_KEY = "Exceptional";
        private IConfigurationSection _exceptionalConfiguration;

        //TODO: We really need to add a settings validation
        public static void ConfigureSettings(IConfiguration configuration, Settings settings)
        {
            var configSettings = new ConfigSettings();
            configSettings._exceptionalConfiguration = configuration.GetSection(CONFIGSECTION_KEY);
            if (configSettings._exceptionalConfiguration.Value != null)
            {
                configSettings._exceptionalConfiguration.Bind(configSettings);
                configSettings.Initialize(settings);
            }
        }

        /// <summary>
        /// Application name to log with.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// The regular expression pattern of data keys to include. 
        /// For example, "Redis.*" would include all keys that start with Redis.
        /// </summary>
        public string DataIncludePattern { get; set; }

        public bool ConfigSectionExists(string setting)
        {
            return _exceptionalConfiguration.GetChildren().Any(x => x.Key == setting);
        }

        public void Initialize(Settings settings)
        {
            // Main settings
            settings.ApplicationName = ApplicationName;
            if (DataIncludePattern.HasValue())
            {
                settings.DataIncludeRegex = new Regex(DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
            }

            if (ConfigSectionExists(nameof(Email)))
                Email.Initialize(settings);
            if (ConfigSectionExists(nameof(ErrorStore)))
                ErrorStore.Initialize(settings);
            if (ConfigSectionExists(nameof(IgnoreErrors)))
                IgnoreErrors.Initialize(settings);
            if (ConfigSectionExists(nameof(LogFilters)))
                LogFilters.Initialize(settings);
        }
    }
}