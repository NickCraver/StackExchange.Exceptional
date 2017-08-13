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

        public ErrorStoreSettings ErrorStore { get; set; }
        public class ErrorStoreSettings
        {
            public string Type { get; set; }

            public string Path { get; set; }

            public string ConnectionString { get; set; }

#if !NETSTANDARD2_0
            public string ConnectionStringName { get; set; }
#endif

            public int? Size { get; set; }

            public int? RollupSeconds { get; set; }

            public int? BackupQueueSize { get; set; }

            internal void Populate(Settings settings)
            {
                var storeSettings = settings.Store;
                if (Type.HasValue()) storeSettings.Type = Type;
                if (Path.HasValue()) storeSettings.Path = Path;
                if (ConnectionString.HasValue()) storeSettings.ConnectionString = ConnectionString;
#if !NETSTANDARD2_0
            if (ConnectionStringName.HasValue()) storeSettings.ConnectionStringName = ConnectionStringName;
#endif
                if (Size.HasValue) storeSettings.Size = Size.Value;
                if (RollupSeconds.HasValue) storeSettings.RollupPeriod = TimeSpan.FromSeconds(RollupSeconds.Value);
                if (BackupQueueSize.HasValue) storeSettings.BackupQueueSize = BackupQueueSize.Value;
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
            public Dictionary<string, string> Form { get; set; } 

            public Dictionary<string, string> Cookies { get; set; }

            internal void Populate(Settings settings)
            {
                var s = settings.LogFilters;
                if (Form != null)
                {
                    foreach (var f in Form)
                    {
                        s.Form[f.Key] = f.Value;
                    }
                }
                if (Cookies != null)
                {
                    foreach (var c in Cookies)
                    {
                        s.Cookie[c.Key] = c.Value;
                    }
                }
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
                if(ToAddress.HasValue()) emailSettings.ToAddress = ToAddress;
                if (FromAddress.HasValue()) emailSettings.FromAddress = FromAddress;
                if (FromDisplayName.HasValue()) emailSettings.FromDisplayName = FromDisplayName;
                if (SMTPHost.HasValue()) emailSettings.SMTPHost = SMTPHost;
                if (SMTPPort.HasValue) emailSettings.SMTPPort = SMTPPort;
                if (SMTPUserName.HasValue()) emailSettings.SMTPUserName = SMTPUserName;
                if (SMTPPassword.HasValue()) emailSettings.SMTPPassword = SMTPPassword;
                if (SMTPEnableSSL.HasValue) emailSettings.SMTPEnableSSL = SMTPEnableSSL.Value;
                if (PreventDuplicates.HasValue) emailSettings.PreventDuplicates = PreventDuplicates.Value;

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
            LogFilters?.Populate(settings);
        }
    }
}