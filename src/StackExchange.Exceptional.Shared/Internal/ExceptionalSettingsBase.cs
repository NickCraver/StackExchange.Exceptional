using Newtonsoft.Json;
using StackExchange.Exceptional.Notifiers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Settings for Exceptional error logging.
    /// </summary>
    public abstract class ExceptionalSettingsBase
    {
        /// <summary>
        /// Event handler to run before an exception is logged to the store.
        /// </summary>
        public event EventHandler<ErrorBeforeLogEventArgs> OnBeforeLog;

        /// <summary>
        /// Event handler to run after an exception has been logged to the store.
        /// </summary>
        public event EventHandler<ErrorAfterLogEventArgs> OnAfterLog;

        /// <summary>
        /// Action to run when we failed to log an exception to the underlying store.
        /// </summary>
        public Action<Exception> OnLogFailure;

        internal bool BeforeLog(Error error, ErrorStore store)
        {
            if (OnBeforeLog != null)
            {
                try
                {
                    var args = new ErrorBeforeLogEventArgs(error);
                    OnBeforeLog(store, args);
                    if (args.Abort) return true;
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
            }
            return false;
        }

        internal void AfterLog(Error error, ErrorStore store)
        {
            if (OnAfterLog != null)
            {
                try
                {
                    OnAfterLog(store, new ErrorAfterLogEventArgs(error));
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
            }
        }

        /// <summary>
        /// Notifiers to run just after an error is logged, like emailing it to a user.
        /// </summary>
        public List<IErrorNotifier> Notifiers { get; } = new List<IErrorNotifier>();

        /// <summary>
        /// Registers a notifier if it's not already registered.
        /// </summary>
        /// <param name="notifier">The <see cref="IErrorNotifier"/> to register.</param>
        public void Register(IErrorNotifier notifier)
        {
            if (!Notifiers.Contains(notifier))
            {
                Notifiers.Add(notifier);
            }
        }

        /// <summary>
        /// Data handlers, for adding any data desirable to an exception before logging, like Commands.
        /// The key here is the full type name, e.g. "System.Data.SqlClient.SqlException"
        /// </summary>
        public Dictionary<string, Action<Error>> ExceptionActions { get; } = new Dictionary<string, Action<Error>>().AddDefault();

        /// <summary>
        /// The <see cref="Regex"/> of data keys to include. For example, "Redis.*" would include all keys that start with Redis.
        /// For options, <see cref="RegexOptions.IgnoreCase"/> and <see cref="RegexOptions.Singleline"/> are recommended.
        /// </summary>
        public Regex DataIncludeRegex { get; set; }

        /// <summary>
        /// Whether to append full stack traces to exceptions. Defaults to true.
        /// </summary>
        public bool AppendFullStackTraces { get; set; } = true;

        /// <summary>
        /// Method to get custom data for an error; will be called when custom data isn't already present.
        /// </summary>
        public Action<Exception, Dictionary<string, string>> GetCustomData { get; set; }

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
            set => _defaultStore = value;
        }

        /// <summary>
        /// Internal fetcher for getting the <see cref="DefaultStore"/>, if it's been initialized.
        /// </summary>
        internal ErrorStore DefaultStoreIfExists => _defaultStore;

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

            /// <summary>
            /// Header values to replace on save - this prevents logging authentication tokens, etc.
            /// The key is the header name to match, the value is what to use when logging.
            /// </summary>
            [JsonConverter(typeof(CaseInsensitiveDictionaryConverter<string>))]
            public Dictionary<string, string> Header { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Query string values to replace on save - this prevents logging authentication tokens, etc.
            /// The key is the query string parameter name to match, the value is what to use when logging.
            /// </summary>
            [JsonConverter(typeof(CaseInsensitiveDictionaryConverter<string>))]
            public Dictionary<string, string> QueryString { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
        /// Creates a new instance of <see cref="ExceptionalSettingsBase"/>.
        /// </summary>
        protected ExceptionalSettingsBase()
        {
            Store.PropertyChanged += (_, __) => _defaultStore = null;
        }
    }

    internal class ExceptionalSettingsDefault : ExceptionalSettingsBase
    {
    }
}
