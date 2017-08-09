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
        /// <summary>
        /// Application name to log with.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// The regular expression pattern of data keys to include. 
        /// For example, "Redis.*" would include all keys that start with Redis.
        /// </summary>
        public string DataIncludePattern { get; set; }


        public static void InitializeSettings(Settings settings, ConfigSettings configSettings)
        {
            // Main settings
            settings.ApplicationName = configSettings.ApplicationName;
            if (configSettings.DataIncludePattern.HasValue())
            {
                settings.DataIncludeRegex = new Regex(configSettings.DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
            }

            var emailSettings = settings.Email;
            emailSettings.ToAddress = configSettings.Email.ToAddress;
            emailSettings.FromAddress = configSettings.Email.FromAddress;
            emailSettings.FromDisplayName = configSettings.Email.FromDisplayName;
            emailSettings.SMTPHost = configSettings.Email.SMTPHost;
            emailSettings.SMTPPort = configSettings.Email.SMTPPort;
            emailSettings.SMTPUserName = configSettings.Email.SMTPUserName;
            emailSettings.SMTPPassword = configSettings.Email.SMTPPassword;
            emailSettings.SMTPEnableSSL = configSettings.Email.SMTPEnableSSL;
            emailSettings.PreventDuplicates = configSettings.Email.PreventDuplicates;

            if (emailSettings.ToAddress.HasValue())
            {
                EmailNotifier.Setup(emailSettings);
            }


            var storeSettings = settings.Store;
            storeSettings.Type = configSettings.ErrorStore.Type;
            storeSettings.Path = configSettings.ErrorStore.Path;
            storeSettings.ConnectionString = configSettings.ErrorStore.ConnectionString;
#if !NETSTANDARD2_0
            storeSettings.ConnectionStringName = configSettings.ErrorStore.ConnectionStringName;
#endif
            storeSettings.Size = configSettings.ErrorStore.Size;
            storeSettings.RollupPeriod = TimeSpan.FromSeconds(configSettings.ErrorStore.RollupSeconds);
            storeSettings.BackupQueueSize = configSettings.ErrorStore.BackupQueueSize;

            var ignoreSettings = settings.Ignore;
            foreach (IgnoreRegex r in configSettings.IgnoreErrors.Regexes)
            {
                ignoreSettings.Regexes.Add(r.PatternRegex);
            }
            foreach (IgnoreType t in configSettings.IgnoreErrors.Types)
            {
                ignoreSettings.Types.Add(t.Type);
            }

            var s = settings.LogFilters;
            foreach (LogFilter f in configSettings.LogFilters.Form)
            {
                s.Form[f.Name] = f.ReplaceWith;
            }
            foreach (LogFilter c in configSettings.LogFilters.Cookies)
            {
                s.Cookie[c.Name] = c.ReplaceWith;
            }
        }
    }
}