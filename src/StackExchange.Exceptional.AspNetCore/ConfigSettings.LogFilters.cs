using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Serialization;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        /// <summary>
        /// The Ignore section of the configuration, optional and no errors will be blocked from logging if not specified.
        /// </summary>
        public LogFilterSettings LogFilters { get; set; } = new LogFilterSettings();

        /// <summary>
        /// Ignore element for deserialization from a configuration, e.g. web.config or app.config
        /// </summary>
        public class LogFilterSettings
        {
            /// <summary>
            /// Form submitted values to replace on save - this prevents logging passwords, etc.
            /// </summary>
            public List<LogFilter> Form { get; set; } = new List<LogFilter>();

            /// <summary>
            /// Cookie values to replace on save - this prevents logging authentication tokens, etc.
            /// </summary>
            public List<LogFilter> Cookies { get; set; } = new List<LogFilter>();

            public void Initialize(Settings settings)
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
        }

        /// <summary>
        /// A filter entry with the form variable name and what to replace the value with when logging.
        /// </summary>
        public class LogFilter
        {
            /// <summary>
            /// The form parameter name to ignore.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The value to log instead of the real value.
            /// </summary>
            public string ReplaceWith { get; set; }
        }
    }
}