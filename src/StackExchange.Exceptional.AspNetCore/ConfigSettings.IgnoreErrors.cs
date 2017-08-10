using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    public partial class ConfigSettings
    {
        /// <summary>
        /// The Ignore section of the configuration, optional and no errors will be blocked from logging if not specified.
        /// </summary>
        public IgnoreSettings IgnoreErrors { get; set; } = new IgnoreSettings();

        /// <summary>
        /// Ignore element for deserialization from a configuration, e.g. web.config or app.config
        /// </summary>
        public class IgnoreSettings
        {
            /// <summary>
            /// Regular expressions collection for errors to ignore.  
            /// Any errors with a .ToString() matching any regular expression here will not be logged.
            /// </summary>
            public List<IgnoreRegex> Regexes { get; set; } = new List<IgnoreRegex>();

            /// <summary>
            /// Types collection for errors to ignore.  Any errors with a Type matching any name here will not be logged
            /// </summary>
            public List<IgnoreType> Types { get; set; } = new List<IgnoreType>();
        }

        /// <summary>
        /// A regular expression entry, to match against error messages to see if we should ignore them.
        /// </summary>
        public class IgnoreRegex
        {
            /// <summary>
            /// The name that describes this regular expression.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The Pattern to match on the exception message.
            /// </summary>
            public string Pattern { get; set; }

            private Regex _patternRegEx;
            /// <summary>
            /// <see cref="Regex"/> object representing the pattern specified, compiled once for use against all future exceptions.
            /// </summary>
            public Regex PatternRegex => _patternRegEx ?? (_patternRegEx = new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline));
        }

        /// <summary>
        /// A type entry, to match against error messages types to see if we should ignore them.
        /// </summary>
        public class IgnoreType
        {
            /// <summary>
            /// The name that describes this ignored type.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The Pattern to match on the exception message.
            /// </summary>
            public string Type { get; set; }
        }
    }
}