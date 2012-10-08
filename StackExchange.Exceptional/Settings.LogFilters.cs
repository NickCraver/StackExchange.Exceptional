using System.Configuration;

namespace StackExchange.Exceptional
{
    public partial class Settings
    {
        /// <summary>
        /// The Ignore section of the configuration, optional and no errors will be blocked from logging if not specified
        /// </summary>
        [ConfigurationProperty("LogFilters")]
        public LogFilterSettings LogFilters
        {
            get { return this["LogFilters"] as LogFilterSettings; }
        }

        /// <summary>
        /// Ignore element for deserilization from a configuration, e.g. web.config or app.config
        /// </summary>
        public class LogFilterSettings : ConfigurationElement
        {
            /// <summary>
            /// Regular expressions collection for errors to ignore.  Any errors with a .ToString() matching any regex here will not be logged
            /// </summary>
            [ConfigurationProperty("Form")]
            public SettingsCollection<LogFilter> FormFilters
            {
                get { return this["Form"] as SettingsCollection<LogFilter>; }
            }
        }
    }

    /// <summary>
    /// A filter entry with the forn variable name and what to replace the value with when logging
    /// </summary>
    public class LogFilter : Settings.SettingsCollectionElement
    {
        /// <summary>
        /// The form parameter name to ignore
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true)]
        public override string Name { get { return this["name"] as string; } }

        /// <summary>
        /// The value to log instead of the real value
        /// </summary>
        [ConfigurationProperty("replaceWith")]
        public string ReplaceWith { get { return this["replaceWith"] as string; } }
    }
}