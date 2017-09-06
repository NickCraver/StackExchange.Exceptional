using Microsoft.Extensions.Configuration;
using StackExchange.Exceptional.Notifiers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Only used for deserialization of settings from IConfiguration, so internal here.
    /// Settings (in shared) is what a user would interface with in their code directly.
    /// </summary>
    internal class ConfigSettings
    {
        public ConfigSettings(IConfiguration configuration)
        {
            configuration.Bind(this);
        }

        public string DataIncludePattern { get; set; }
        public bool? UseExceptionalPageOnThrow { get; set; }

        public ErrorStoreSettings ErrorStore { get; set; }
        public class ErrorStoreSettings
        {
            public string ApplicationName { get; set; }
            public string Type { get; set; }
            public string Path { get; set; }
            public string ConnectionString { get; set; }
#if !NETSTANDARD2_0
            public string ConnectionStringName { get; set; }
#endif
            public int? Size { get; set; }
            public int? RollupSeconds { get; set; }
            public int? BackupQueueSize { get; set; }

            internal void Populate(ExceptionalSettings settings)
            {
                var storeSettings = settings.Store;
                if (ApplicationName.HasValue()) storeSettings.ApplicationName = ApplicationName;
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

            internal void Populate(ExceptionalSettings settings)
            {
                var s = settings.Ignore;
                if (Regexes != null)
                {
                    foreach (var regex in Regexes)
                    {
                        if (regex.HasValue())
                        {
                            s.Regexes.Add(new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Singleline));
                        }
                    }
                }
                if (Types != null)
                {
                    foreach (var type in Types)
                    {
                        s.Types.Add(type);
                    }
                }
            }
        }

        public LogFilterSettings LogFilters { get; set; }
        public class LogFilterSettings
        {
            public Dictionary<string, string> Form { get; set; }
            public Dictionary<string, string> Cookies { get; set; }

            internal void Populate(ExceptionalSettings settings)
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

            internal void Populate(ExceptionalSettings settings)
            {
                var s = settings.Email;
                if (ToAddress.HasValue()) s.ToAddress = ToAddress;
                if (FromAddress.HasValue()) s.FromAddress = FromAddress;
                if (FromDisplayName.HasValue()) s.FromDisplayName = FromDisplayName;
                if (SMTPHost.HasValue()) s.SMTPHost = SMTPHost;
                if (SMTPPort.HasValue) s.SMTPPort = SMTPPort;
                if (SMTPUserName.HasValue()) s.SMTPUserName = SMTPUserName;
                if (SMTPPassword.HasValue()) s.SMTPPassword = SMTPPassword;
                if (SMTPEnableSSL.HasValue) s.SMTPEnableSSL = SMTPEnableSSL.Value;
                if (PreventDuplicates.HasValue) s.PreventDuplicates = PreventDuplicates.Value;

                if (s.ToAddress.HasValue())
                {
                    settings.Register(new EmailNotifier(s));
                }
            }
        }

        internal void Populate(ExceptionalSettings settings)
        {
            if (DataIncludePattern.HasValue())
            {
                settings.DataIncludeRegex = new Regex(DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
            }
            if (UseExceptionalPageOnThrow.HasValue)
            {
                settings.UseExceptionalPageOnThrow = UseExceptionalPageOnThrow.Value;
            }

            ErrorStore?.Populate(settings);
            IgnoreErrors?.Populate(settings);
            LogFilters?.Populate(settings);
            Email?.Populate(settings);
        }
    }
}