using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Notifiers;
using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// The Settings element for Exceptional's configuration.
    /// This is the legacy web.config settings, that only serve as an adapter to populate <see cref="ExceptionalSettings"/>.
    /// </summary>
    internal class Settings : ConfigurationSection
    {
        private static int _loaded;
        /// <summary>
        /// Trigger deserialization, which loads settings from the .config file.
        /// </summary>
        public static void LoadSettings()
        {
            if (Interlocked.CompareExchange(ref _loaded, 1, 0) == 0)
            {
                var settings = new ExceptionalSettings();

                if (ConfigurationManager.GetSection("Exceptional") is Settings config)
                {
                    if (config.DataIncludePattern.HasValue())
                    {
                        settings.DataIncludeRegex = new Regex(config.DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
                    }

                    config.ErrorStore?.Populate(settings);
                    config.Ignore?.Populate(settings);
                    config.LogFilters?.Populate(settings);
                    config.Email?.Populate(settings);
                    settings.Store.ApplicationName = config.ApplicationName;
                }

                Exceptional.Configure(settings);
            }
        }

        [ConfigurationProperty("applicationName", IsRequired = true)]
        public string ApplicationName => this["applicationName"] as string;
        [ConfigurationProperty("dataIncludePattern")]
        public string DataIncludePattern => this["dataIncludePattern"] as string;

        [ConfigurationProperty("ErrorStore")]
        public ErrorStoreSettings ErrorStore => this["ErrorStore"] as ErrorStoreSettings;
        public class ErrorStoreSettings : ExceptionalElement
        {
            [ConfigurationProperty("type", IsRequired = true)]
            public string Type => Get("type");
            [ConfigurationProperty("path")]
            public string Path => Get("path");
            [ConfigurationProperty("connectionString")]
            public string ConnectionString => Get("connectionString");
            [ConfigurationProperty("connectionStringName")]
            public string ConnectionStringName => Get("connectionStringName");
            [ConfigurationProperty("size")]
            public int? Size => GetInt("size");
            [ConfigurationProperty("rollupSeconds")]
            public int? RollupSeconds => GetInt("rollupSeconds");
            [ConfigurationProperty("backupQueueSize")]
            public int? BackupQueueSize => GetInt("backupQueueSize");

            internal void Populate(ExceptionalSettings settings)
            {
                var s = settings.Store;
                s.Type = Type;
                if (Path.HasValue()) s.Path = Path;
                if (ConnectionString.HasValue()) s.ConnectionString = ConnectionString;
                if (ConnectionStringName.HasValue())
                {
                    s.ConnectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName]?.ConnectionString
                        ?? throw new ConfigurationErrorsException("A connection string was not found for the connection string name provided: " + ConnectionStringName);
                }
                if (Size.HasValue) s.Size = Size.Value;
                if (RollupSeconds.HasValue) s.RollupPeriod = TimeSpan.FromSeconds(RollupSeconds.Value);
                if (BackupQueueSize.HasValue) s.BackupQueueSize = BackupQueueSize.Value;
            }
        }

        [ConfigurationProperty("IgnoreErrors")]
        public IgnoreSettings Ignore => this["IgnoreErrors"] as IgnoreSettings;
        public class IgnoreSettings : ConfigurationElement
        {
            [ConfigurationProperty("Regexes")]
            public SettingsCollection<IgnoreRegex> Regexes => this["Regexes"] as SettingsCollection<IgnoreRegex>;
            [ConfigurationProperty("Types")]
            public SettingsCollection<IgnoreType> Types => this["Types"] as SettingsCollection<IgnoreType>;

            /// <summary>
            /// Runs after deserialization, to populate <see cref="ExceptionalSettingsBase.Ignore"/>.
            /// </summary>
            /// <param name="settings">The <see cref="ExceptionalSettings"/> to populate.</param>
            internal void Populate(ExceptionalSettings settings)
            {
                var s = settings.Ignore;
                foreach (IgnoreRegex r in Regexes)
                {
                    if (r.Pattern.HasValue())
                    {
                        s.Regexes.Add(new Regex(r.Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline));
                    }
                }
                foreach (IgnoreType t in Types)
                {
                    s.Types.Add(t.Type);
                }
            }

            internal class IgnoreRegex : SettingsCollectionElement
            {
                [ConfigurationProperty("name")]
                public override string Name => Get("name");
                [ConfigurationProperty("pattern", IsRequired = true)]
                public string Pattern => Get("pattern");
            }

            internal class IgnoreType : SettingsCollectionElement
            {
                [ConfigurationProperty("name")]
                public override string Name => Get("name");
                [ConfigurationProperty("type", IsRequired = true)]
                public string Type => Get("type");
            }
        }

        [ConfigurationProperty("LogFilters")]
        public LogFilterSettings LogFilters => this["LogFilters"] as LogFilterSettings;
        public class LogFilterSettings : ExceptionalElement
        {
            [ConfigurationProperty("Form")]
            public SettingsCollection<LogFilter> FormFilters => this["Form"] as SettingsCollection<LogFilter>;
            [ConfigurationProperty("Cookies")]
            public SettingsCollection<LogFilter> CookieFilters => this["Cookies"] as SettingsCollection<LogFilter>;
            [ConfigurationProperty("Headers")]
            public SettingsCollection<LogFilter> HeaderFilters => this["Headers"] as SettingsCollection<LogFilter>;
            [ConfigurationProperty("QueryString")]
            public SettingsCollection<LogFilter> QueryStringFilters => this["QueryString"] as SettingsCollection<LogFilter>;

            internal void Populate(ExceptionalSettings settings)
            {
                var s = settings.LogFilters;
                foreach (LogFilter f in FormFilters)
                {
                    s.Form[f.Name] = f.ReplaceWith;
                }
                foreach (LogFilter c in CookieFilters)
                {
                    s.Cookie[c.Name] = c.ReplaceWith;
                }
                foreach (LogFilter h in HeaderFilters)
                {
                    s.Header[h.Name] = h.ReplaceWith;
                }
                foreach (LogFilter h in QueryStringFilters)
                {
                    s.QueryString[h.Name] = h.ReplaceWith;
                }
            }

            internal class LogFilter : SettingsCollectionElement
            {
                [ConfigurationProperty("name", IsRequired = true)]
                public override string Name => Get("name");
                [ConfigurationProperty("replaceWith")]
                internal string ReplaceWith => Get("replaceWith");
            }
        }

        [ConfigurationProperty("Email")]
        public EmailSettingsConfig Email => this["Email"] as EmailSettingsConfig;
        public class EmailSettingsConfig : ExceptionalElement
        {
            [ConfigurationProperty("toAddress", IsRequired = true)]
            public string ToAddress => Get("toAddress");
            [ConfigurationProperty("fromAddress")]
            public string FromAddress => Get("fromAddress");
            [ConfigurationProperty("fromDisplayName")]
            public string FromDisplayName => Get("fromDisplayName");
            [ConfigurationProperty("smtpHost")]
            public string SMTPHost => Get("smtpHost");
            [ConfigurationProperty("smtpPort")]
            public int? SMTPPort => GetInt("smtpPort");
            [ConfigurationProperty("smtpUserName")]
            public string SMTPUserName => Get("smtpUserName");
            [ConfigurationProperty("smtpPassword")]
            public string SMTPPassword => Get("smtpPassword");
            [ConfigurationProperty("smtpEnableSsl")]
            public bool? SMTPEnableSSL => GetBool("smtpEnableSsl");
            [ConfigurationProperty("preventDuplicates")]
            public bool? PreventDuplicates => GetBool("preventDuplicates");

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

        /// <summary>
        /// A collection of list types all with a Name attribute.
        /// </summary>
        /// <typeparam name="T">The type of collection, inherited from SettingsCollectionElement.</typeparam>
        public class SettingsCollection<T> : ConfigurationElementCollection where T : SettingsCollectionElement, new()
        {
            /// <summary>
            /// Accessor by key.
            /// </summary>
            /// <param name="key">The key to lookup.</param>
            public new T this[string key] => BaseGet(key) as T;

            /// <summary>
            /// Accessor by index.
            /// </summary>
            /// <param name="index">The index position to lookup.</param>
            public T this[int index] => BaseGet(index) as T;

            /// <summary>
            /// Default constructor for this element.
            /// </summary>
            protected override ConfigurationElement CreateNewElement() => new T();

            /// <summary>
            /// Default by-key fetch for this element.
            /// </summary>
            /// <param name="element">The element to get a key for.</param>
            protected override object GetElementKey(ConfigurationElement element) => element.ToString();
        }

        /// <summary>
        /// An element in a settings collection that has a Name property, a generic base for SettingsCollection collections.
        /// </summary>
        internal abstract class SettingsCollectionElement : ExceptionalElement
        {
            public override string ToString() => Name;
            public abstract string Name { get; }
        }

        internal class ExceptionalElement : ConfigurationElement
        {
            public string Get(string name) => this[name] as string;
            public bool? GetBool(string name) => (bool?)this[name];
            public int? GetInt(string name) => (int?)this[name];
        }
    }
}
