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

        //TODO: We really need to add a settings validation
        public static Settings LoadSettings(IConfiguration configuration)
        {
            var configSettings = new ConfigSettings();
            var settings = new Settings();
            configuration.GetSection(CONFIGSECTION_KEY).Bind(configSettings);            
            configSettings.InitializeSettings(settings);
            return settings;
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


        public void InitializeSettings(Settings settings)
        {
            // Main settings
            settings.ApplicationName = ApplicationName;
            if (DataIncludePattern.HasValue())
            {
                settings.DataIncludeRegex = new Regex(DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
            }

            var emailSettings = settings.Email;
            emailSettings.ToAddress = Email.ToAddress;
            emailSettings.FromAddress = Email.FromAddress;
            emailSettings.FromDisplayName = Email.FromDisplayName;
            emailSettings.SMTPHost = Email.SMTPHost;
            emailSettings.SMTPPort = Email.SMTPPort;
            emailSettings.SMTPUserName = Email.SMTPUserName;
            emailSettings.SMTPPassword = Email.SMTPPassword;
            emailSettings.SMTPEnableSSL = Email.SMTPEnableSSL;
            emailSettings.PreventDuplicates = Email.PreventDuplicates;

            if (emailSettings.ToAddress.HasValue())
            {
                EmailNotifier.Setup(emailSettings);
            }


            var storeSettings = settings.Store;
            storeSettings.Type = ErrorStore.Type;
            storeSettings.Path = ErrorStore.Path;
            storeSettings.ConnectionString = ErrorStore.ConnectionString;
#if !NETSTANDARD2_0
            storeSettings.ConnectionStringName = ErrorStore.ConnectionStringName;
#endif
            storeSettings.Size = ErrorStore.Size;
            storeSettings.RollupPeriod = TimeSpan.FromSeconds(ErrorStore.RollupSeconds);
            storeSettings.BackupQueueSize = ErrorStore.BackupQueueSize;

            var ignoreSettings = settings.Ignore;
            foreach (IgnoreRegex r in IgnoreErrors.Regexes)
            {
                ignoreSettings.Regexes.Add(r.PatternRegex);
            }
            foreach (IgnoreType t in IgnoreErrors.Types)
            {
                ignoreSettings.Types.Add(t.Type);
            }

            var s = settings.LogFilters;
            foreach (LogFilter f in LogFilters.Form)
            {
                s.Form[f.Name] = f.ReplaceWith;
            }
            foreach (LogFilter c in LogFilters.Cookies)
            {
                s.Cookie[c.Name] = c.ReplaceWith;
            }
        }
    }
}