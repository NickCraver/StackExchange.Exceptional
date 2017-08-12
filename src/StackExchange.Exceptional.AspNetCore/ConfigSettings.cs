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
    internal partial class ConfigSettings
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
                configSettings.Populate(settings);
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

        public bool SectionExists(string setting)
        {
            return _exceptionalConfiguration.GetChildren().Any(x => x.Key == setting);
        }

        public void Populate(Settings settings)
        {
            // Main settings
            settings.ApplicationName = ApplicationName;
            if (DataIncludePattern.HasValue())
            {
                settings.DataIncludeRegex = new Regex(DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
            }

            if (SectionExists(nameof(Email)))
                Email.Populate(settings);
            if (SectionExists(nameof(ErrorStore)))
                ErrorStore.Populate(settings);
            if (SectionExists(nameof(IgnoreErrors)))
                IgnoreErrors.Populate(settings);
            if (SectionExists(nameof(LogFilters)))
                LogFilters.Populate(settings);
        }
    }
}