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
    internal partial class ConfigSettings
    {
        public ConfigSettings(IConfiguration configuration)
        {
            configuration.Bind(this);
        }

        public string ApplicationName { get; set; }
        public string DataIncludePattern { get; set; }

        internal void Populate(Settings settings)
        {
            settings.ApplicationName = ApplicationName ?? settings.ApplicationName;
            if (DataIncludePattern.HasValue())
            {
                settings.DataIncludeRegex = new Regex(DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
            }

            Email?.Populate(settings);
            ErrorStore?.Populate(settings);
            IgnoreErrors?.Populate(settings);
            LogFilters.Populate(settings);
        }
    }
}