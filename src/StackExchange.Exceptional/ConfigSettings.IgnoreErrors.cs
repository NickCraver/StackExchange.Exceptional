using System.Configuration;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        /// <summary>
        /// The Ignore section of the configuration, optional and no errors will be blocked from logging if not specified.
        /// </summary>
        [ConfigurationProperty("IgnoreErrors")]
        public IgnoreSettings Ignore => this["IgnoreErrors"] as IgnoreSettings;

        /// <summary>
        /// Ignore element for deserialization from a configuration, e.g. web.config or app.config
        /// </summary>
        public class IgnoreSettings : ConfigurationElement
        {
            /// <summary>
            /// Regular expressions collection for errors to ignore.  
            /// Any errors with a .ToString() matching any regular expression here will not be logged.
            /// </summary>
            [ConfigurationProperty("Regexes")]
            public SettingsCollection<IgnoreRegex> Regexes => this["Regexes"] as SettingsCollection<IgnoreRegex>;

            /// <summary>
            /// Types collection for errors to ignore.  Any errors with a Type matching any name here will not be logged
            /// </summary>
            [ConfigurationProperty("Types")]
            public SettingsCollection<IgnoreType> Types => this["Types"] as SettingsCollection<IgnoreType>;

            /// <summary>
            /// Runs after deserialization, to populate <see cref="Settings.Ignore"/>.
            /// </summary>
            protected override void PostDeserialize()
            {
                base.PostDeserialize();

                var s = Settings.Current.Ignore;
                foreach (IgnoreRegex r in Regexes)
                {
                    s.Regexes.Add(r.PatternRegex);
                }
                foreach (IgnoreType t in Types)
                {
                    s.Types.Add(t.Type);
                }
            }
        }

        /// <summary>
        /// A regular expression entry, to match against error messages to see if we should ignore them.
        /// </summary>
        public class IgnoreRegex : SettingsCollectionElement
        {
            /// <summary>
            /// The name that describes this regular expression.
            /// </summary>
            [ConfigurationProperty("name")]
            public override string Name => this["name"] as string;

            /// <summary>
            /// The Pattern to match on the exception message.
            /// </summary>
            [ConfigurationProperty("pattern", IsRequired = true)]
            public string Pattern => this["pattern"] as string;

            private Regex _patternRegEx;
            /// <summary>
            /// <see cref="Regex"/> object representing the pattern specified, compiled once for use against all future exceptions.
            /// </summary>
            public Regex PatternRegex => _patternRegEx ?? (_patternRegEx = new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline));
        }

        /// <summary>
        /// A type entry, to match against error messages types to see if we should ignore them.
        /// </summary>
        public class IgnoreType : SettingsCollectionElement
        {
            /// <summary>
            /// The name that describes this ignored type.
            /// </summary>
            [ConfigurationProperty("name")]
            public override string Name => this["name"] as string;

            /// <summary>
            /// The fully qualified type of the exception to ignore.
            /// </summary>
            [ConfigurationProperty("type", IsRequired = true)]
            public string Type => this["type"] as string;
        }
    }
}