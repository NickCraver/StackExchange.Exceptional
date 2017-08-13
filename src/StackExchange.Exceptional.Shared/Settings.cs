using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Notifiers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Settings for Exceptional error logging.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Current instance of the settings element.
        /// </summary>
        public static Settings Current { get; set; } = new Settings();

        /// <summary>
        /// Notifiers to run just after an error is logged, like emailing it to a user.
        /// </summary>
        public List<IErrorNotifier> Notifiers { get; } = new List<IErrorNotifier>();

        /// <summary>
        /// Data handlers, for adding any data desirable to an exception before logging, like Commands.
        /// The key here is the full type name, e.g. "System.Data.SqlClient.SqlException"
        /// </summary>
        public Dictionary<string, Action<Error>> ExceptionActions { get; } = new Dictionary<string, Action<Error>>().AddDefault();

        /// <summary>
        /// Application name to log with.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// The <see cref="Regex"/> of data keys to include. For example, "Redis.*" would include all keys that start with Redis.
        /// For options, <see cref="RegexOptions.IgnoreCase"/> and <see cref="RegexOptions.Singleline"/> are recommended.
        /// </summary>
        public Regex DataIncludeRegex { get; set; }

        /// <summary>
        /// Returns whether an error passed in right now would be logged.
        /// </summary>
        public static bool IsLoggingEnabled { get; private set; } = true;

        /// <summary>
        /// Re-enables error logging after a <see cref="DisableLogging"/> call.
        /// </summary>
        public static void EnableLogging() => IsLoggingEnabled = true;

        /// <summary>
        /// Disables error logging, call <see cref="EnableLogging"/> to re-enable.
        /// </summary>
        /// <remarks>
        /// This is useful when an <see cref="AppDomain"/> is being torn down, for example <code>IRegisteredObject.Stop()</code> when a web application is being stopped
        /// </remarks>
        public static void DisableLogging() => IsLoggingEnabled = false;

        /// <summary>
        /// Method of getting the IP address for the error, defaults to retrieving it from server variables.
        /// but may need to be replaced in special multi-proxy situations.
        /// </summary>
        public Func<string> GetIPAddress { get; set; }

        /// <summary>
        /// Whether to append full stack traces to exceptions. Defaults to true.
        /// </summary>
        public bool AppendFullStackTraces { get; set; } = true;

        /// <summary>
        /// Method to get custom data for an error; will be called when custom data isn't already present.
        /// </summary>
        public Action<Exception, Dictionary<string, string>> GetCustomData { get; set; }

        /// <summary>
        /// ASP.NET Core Only!
        /// Whether to show the Exceptional page on throw, instead of the built-in .UseDeveloperExceptionPage()
        /// </summary>
        public bool UseExceptionalPageOnThrow { get; set; }

        /// <summary>
        /// Settings for the rendering of pages.
        /// </summary>
        public RenderSettings Render { get; } = new RenderSettings();

        /// <summary>
        /// Settings for the rendering of pages.
        /// </summary>
        public class RenderSettings
        {
            /// <summary>
            /// A list of a JavaScript files to include to all error log pages, for customizing the behavior and such.
            /// Be sure to resolve the path before passing it in here, as it will be rendered literally in the &lt;script src="" attribute.
            /// </summary>
            public List<string> JSIncludes { get; } = new List<string>();

            /// <summary>
            /// Adds a CSS include to all error log pages, for customizing the look and feel.
            /// Be sure to resolve the path before passing it in here, as it will be rendered literally in the &lt;link href="" attribute.
            /// </summary>
            public List<string> CSSIncludes { get; } = new List<string>();
        }

        private ErrorStore _defaultStore;
        /// <summary>
        /// Gets the default error store specified in the configuration, 
        /// or the in-memory store if none is configured.
        /// </summary>
        public ErrorStore DefaultStore
        {
            get => _defaultStore ?? (_defaultStore = ErrorStore.Get(Store));
            internal set => _defaultStore = value;
        }

        /// <summary>
        /// The ErrorStore section of the configuration, optional and will default to a MemoryErrorStore if not specified.
        /// </summary>
        public ErrorStoreSettings Store { get; } = new ErrorStoreSettings();

        /// <summary>
        /// Ignore settings, for filtering out exceptions which aren't wanted.
        /// </summary>
        public IgnoreSettings Ignore { get; } = new IgnoreSettings();

        /// <summary>
        /// Ignore settings, for filtering out exceptions which aren't wanted.
        /// </summary>
        public class IgnoreSettings
        {
            /// <summary>
            /// Regular expressions collection for errors to ignore.  
            /// Any errors with a .ToString() matching any <see cref="Regex"/> here will not be logged.
            /// </summary>
            public HashSet<Regex> Regexes { get; set; } = new HashSet<Regex>();

            /// <summary>
            /// Types collection for errors to ignore.  
            /// Any errors with a Type matching any name here will not be logged.
            /// </summary>
            public HashSet<string> Types { get; set; } = new HashSet<string>();
        }

        /// <summary>
        /// Log filters, for filtering out form and cookie values to prevent logging sensitive data.
        /// </summary>
        public LogFilterSettings LogFilters { get; } = new LogFilterSettings();

        /// <summary>
        /// Log filters, for filtering out form and cookie values to prevent logging sensitive data
        /// </summary>
        public class LogFilterSettings
        {
            /// <summary>
            /// Form submitted values to replace on save - this prevents logging passwords, etc.
            /// The key is the form value to match, the value is what to replace it with when logging.
            /// </summary>
            public Dictionary<string, string> Form { get; set; } = new Dictionary<string, string>();

            /// <summary>
            /// Cookie values to replace on save - this prevents logging authentication tokens, etc.
            /// The key is the cookie name to match, the value is what to use when logging.
            /// </summary>
            public Dictionary<string, string> Cookie { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// Settings for controlling email sending
        /// </summary>
        public EmailSettings Email { get; } = new EmailSettings();

        /// <summary>
        /// Settings for prettifying a StackTrace
        /// </summary>
        public StackTraceSettings StackTrace { get; } = new StackTraceSettings();

        /// <summary>
        /// Settings for prettifying a StackTrace
        /// </summary>
        public class StackTraceSettings
        {
            /// <summary>
            /// Replaces generic names like Dictionary`2 with Dictionary&lt;TKey,TValue&gt;.
            /// Specific formatting is based on the <see cref="Language"/> setting.
            /// </summary>
            public bool EnablePrettyGenerics { get; set; } = true;
            /// <summary>
            /// The language to use when prettifying StackTrace generics.
            /// Defaults to C#.
            /// </summary>
            public CodeLanguage Language { get; set; }
            /// <summary>
            /// Whether to print generic type names like &lt;T1, T2&gt; etc. or just use commas, e.g. &lt;,,&gt; if <see cref="Language"/> is C#.
            /// Defaults to true.
            /// </summary>
            public bool IncludeGenericTypeNames { get; set; } = true;
        }

        /// <summary>
        /// The language to use when operating on errors and stack traces.
        /// </summary>
        public enum CodeLanguage
        {
            /// <summary>
            /// C#
            /// </summary>
            CSharp,
            /// <summary>
            /// F#
            /// </summary>
            FSharp,
            /// <summary>
            /// Visual Basic
            /// </summary>
            VB
        }
    }

    /// <summary>
    /// A settings object got setting up an error store.
    /// </summary>
    public class ErrorStoreSettings
    {
        /// <summary>
        /// The type of error store to use, File, SQL, Memory, etc.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// For file-based error stores.
        /// The path to use on for file storage.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// For database-based error stores.
        /// The connection string to use.  If provided, ConnectionStringName is ignored.
        /// </summary>
        public string ConnectionString { get; set; }

#if !NETSTANDARD2_0
        /// <summary>
        /// For database-based error stores.
        /// The name of the connection string to use from the application's configuration.
        /// </summary>
        public string ConnectionStringName { get; set; }
#endif

        /// <summary>
        /// The size of this error log, either how many to keep or how many to display depending on type.
        /// Defaults to 200.
        /// </summary>
        public int Size { get; set; } = 200;

        /// <summary>
        /// The duration of error groups to roll-up, similar errors within this timespan (those with the same stack trace) will be shown as duplicates.
        /// Defaults to 10 minutes.
        /// </summary>
        public TimeSpan? RollupPeriod { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// The size of the backup queue to use for the log, after roll-ups, it's how many entries in memory can be stored before culling the oldest.
        /// Defaults to 1000.
        /// </summary>
        public int BackupQueueSize { get; set; } = 1000;

        /// <summary>
        /// When a connection to the error store failed, how often to retry logging the errors in queue for logging.
        /// </summary>
        public TimeSpan BackupQueueRetryInterval { get; set; } = TimeSpan.FromSeconds(2);
    }

    /// <summary>
    /// Email settings configuration, for configuring Email sending from code.
    /// </summary>
    public class EmailSettings
    {
        private string _fromAddress, _fromDisplayName,
                       _SMTPUserName, _SMTPPassword;

        internal MailAddress FromMailAddress { get; private set; }
        internal NetworkCredential SMTPCredentials { get; private set; }

        private void SetMailAddress()
        {
            try
            {
                // Because MailAddress.TryParse() isn't a thing, and an invalid address will throw.
                FromMailAddress = _fromDisplayName.HasValue()
                                  ? new MailAddress(_fromAddress, _fromDisplayName)
                                  : new MailAddress(_fromAddress);
            }
            catch
            {
                FromMailAddress = null;
            }
        }

        private void SetCredentials() =>
            SMTPCredentials = _SMTPUserName.HasValue() && _SMTPPassword.HasValue()
                              ? new NetworkCredential(_SMTPUserName, _SMTPPassword)
                              : null;

        /// <summary>
        /// The address to send email messages to.
        /// </summary>
        public string ToAddress { get; set; }
        /// <summary>
        /// The address to send email messages from.
        /// </summary>
        public string FromAddress
        {
            get => _fromAddress;
            set
            {
                _fromAddress = value;
                SetMailAddress();
            }
        }
        /// <summary>
        /// The display name to send email messages from.
        /// </summary>
        public string FromDisplayName
        {
            get => _fromDisplayName;
            set
            {
                _fromDisplayName = value;
                SetMailAddress();
            }
        }
        /// <summary>
        /// The SMTP server to send mail through.
        /// </summary>
        public string SMTPHost { get; set; }
        /// <summary>
        /// The port to send mail on (if SMTP server is specified via <see cref="SMTPHost"/>).
        /// </summary>
        public int? SMTPPort { get; set; }
        /// <summary>
        /// The SMTP user name to use, if authentication is needed.
        /// </summary>
        public string SMTPUserName
        {
            get => _SMTPUserName;
            set
            {
                _SMTPUserName = value;
                SetCredentials();
            }
        }
        /// <summary>
        /// The SMTP password to use, if authentication is needed.
        /// </summary>
        public string SMTPPassword
        {
            get => _SMTPPassword;
            set
            {
                _SMTPPassword = value;
                SetCredentials();
            }
        }
        /// <summary>
        /// Whether to use SSL when sending via SMTP.
        /// </summary>
        public bool SMTPEnableSSL { get; set; }
        /// <summary>
        /// Flags whether or not emails are sent for duplicate errors.
        /// </summary>
        public bool PreventDuplicates { get; set; }
    }
}