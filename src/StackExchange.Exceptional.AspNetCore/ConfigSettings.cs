﻿using Microsoft.Extensions.Configuration;
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

        public ErrorStoreSettings ErrorStore { get; set; }
        public class ErrorStoreSettings
        {
            public string Type { get; set; }

            public string Path { get; set; }

            public string ConnectionString { get; set; }

            public string ConnectionStringName { get; set; }

            public int? Size { get; set; }

            public int? RollupSeconds { get; set; }

            public int? BackupQueueSize { get; set; }

            internal void Populate(Settings settings)
            {
                var storeSettings = settings.Store;
                storeSettings.Type = Type ?? storeSettings.Type;
                storeSettings.Path = Path ?? storeSettings.Path;
                storeSettings.ConnectionString = ConnectionString ?? storeSettings.ConnectionString;
#if !NETSTANDARD2_0
            storeSettings.ConnectionStringName = ConnectionStringName ?? storeSettings.ConnectionStringName;
#endif
                storeSettings.Size = Size ?? storeSettings.Size;
                if (RollupSeconds != null)
                    storeSettings.RollupPeriod = TimeSpan.FromSeconds(RollupSeconds.Value);
                storeSettings.BackupQueueSize = BackupQueueSize ?? storeSettings.BackupQueueSize;
            }
        }

        public IgnoreSettings IgnoreErrors { get; set; }
        public class IgnoreSettings
        {
            public List<string> Regexes { get; set; }

            public List<string> Types { get; set; }

            internal void Populate(Settings settings)
            {
                var ignoreSettings = settings.Ignore;

                if (Regexes != null)
                {
                    foreach (var regex in Regexes)
                    {
                        if (regex.HasValue())
                        {
                            ignoreSettings.Regexes.Add(new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Singleline));
                        }
                    }
                }

                if (Types != null)
                {
                    foreach (var type in Types)
                    {
                        ignoreSettings.Types.Add(type);
                    }
                }
            }
        }

        public LogFilterSettings LogFilters { get; set; }
        public class LogFilterSettings
        {
            public List<LogFilter> Form { get; set; } = new List<LogFilter>();

            public List<LogFilter> Cookies { get; set; } = new List<LogFilter>();

            internal void Populate(Settings settings)
            {
                var s = settings.LogFilters;
                foreach (LogFilter f in Form)
                {
                    s.Form[f.Name] = f.ReplaceWith;
                }
                foreach (LogFilter c in Cookies)
                {
                    s.Cookie[c.Name] = c.ReplaceWith;
                }
            }

            public class LogFilter
            {
                public string Name { get; set; }

                public string ReplaceWith { get; set; }
            }
        }

        public EmailSettingsConfig Email { get; set; }
        public class EmailSettingsConfig
        {
            public string ToAddress { get; set; }

            public string FromAddress { get; set; }

            public string FromDisplayName { get; set; }

            public string SMTPHost { get; set; }

            public int? SMTPPort { get; set; }

            public string SMTPUserName { get; set; }

            public string SMTPPassword { get; set; }

            public bool? SMTPEnableSSL { get; set; }

            public bool? PreventDuplicates { get; set; }

            internal void Populate(Settings settings)
            {
                var emailSettings = settings.Email;
                emailSettings.ToAddress = ToAddress ?? emailSettings.ToAddress;
                emailSettings.FromAddress = FromAddress ?? emailSettings.FromAddress;
                emailSettings.FromDisplayName = FromDisplayName ?? emailSettings.FromDisplayName;
                emailSettings.SMTPHost = SMTPHost ?? emailSettings.SMTPHost;
                emailSettings.SMTPPort = SMTPPort ?? emailSettings.SMTPPort;
                emailSettings.SMTPUserName = SMTPUserName ?? emailSettings.SMTPUserName;
                emailSettings.SMTPPassword = SMTPPassword ?? emailSettings.SMTPPassword;
                emailSettings.SMTPEnableSSL = SMTPEnableSSL ?? emailSettings.SMTPEnableSSL;
                emailSettings.PreventDuplicates = PreventDuplicates ?? emailSettings.PreventDuplicates;

                if (emailSettings.ToAddress.HasValue())
                {
                    EmailNotifier.Setup(emailSettings);
                }
            }
        }

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