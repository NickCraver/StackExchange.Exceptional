using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        public IgnoreSettings IgnoreErrors { get; set; } 

        public class IgnoreSettings
        {
            public List<IgnoreRegex> Regexes { get; set; } = new List<IgnoreRegex>();

            public List<IgnoreType> Types { get; set; } = new List<IgnoreType>();

            internal void Populate(Settings settings)
            {
                var ignoreSettings = settings.Ignore;
                foreach (IgnoreRegex r in Regexes)
                {
                    if (r.Pattern.HasValue())
                    {
                        ignoreSettings.Regexes.Add(new Regex(r.Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline));
                    }
                }
                foreach (IgnoreType t in Types)
                {
                    ignoreSettings.Types.Add(t.Type);
                }
            }
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