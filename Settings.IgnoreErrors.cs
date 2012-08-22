using System.Configuration;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    public partial class Settings
    {
        [ConfigurationProperty("IgnoreErrors")]
        public IgnoreSettings Ignore
        {
            get { return this["IgnoreErrors"] as IgnoreSettings; }
        }

        public class IgnoreSettings : ConfigurationElement
        {
            [ConfigurationProperty("Regexes")]
            public SettingsCollection<IgnoreRegex> Regexes
            {
                get { return this["Regexes"] as SettingsCollection<IgnoreRegex>; }
            }

            [ConfigurationProperty("Types")]
            public SettingsCollection<IgnoreType> Types
            {
                get { return this["Types"] as SettingsCollection<IgnoreType>; }
            }

            protected override void PostDeserialize()
            {
                // noithing for the moment...
            }
        }
    }

    /// <summary>
    /// A regex entry, to match against error messages to see if we should ignore them
    /// </summary>
    public class IgnoreRegex : Settings.SettingsCollectionElement
    {
        /// <summary>
        /// The name that describes this regex
        /// </summary>
        [ConfigurationProperty("name")]
        public override string Name { get { return this["name"] as string; } }

        /// <summary>
        /// The Pattern to match on the exception message
        /// </summary>
        [ConfigurationProperty("pattern", IsRequired = true)]
        public string Pattern { get { return this["pattern"] as string; } }

        private Regex _patternRegEx;
        public Regex PatternRegex
        {
            get { return _patternRegEx ?? (_patternRegEx = new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline)); }
        }
    }

    /// <summary>
    /// A type entry, to match against error messages types to see if we should ignore them
    /// </summary>
    public class IgnoreType : Settings.SettingsCollectionElement
    {
        /// <summary>
        /// The name that describes this ignored type
        /// </summary>
        [ConfigurationProperty("name")]
        public override string Name { get { return this["name"] as string; } }

        /// <summary>
        /// The fully qualified type of the exception to ignore
        /// </summary>
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type { get { return this["type"] as string; } }
    }
}