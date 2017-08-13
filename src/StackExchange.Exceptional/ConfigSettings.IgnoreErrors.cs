using StackExchange.Exceptional.Internal;
using System.Configuration;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        [ConfigurationProperty("IgnoreErrors")]
        public IgnoreSettings Ignore => this["IgnoreErrors"] as IgnoreSettings;

        public class IgnoreSettings : ConfigurationElement
        {
            [ConfigurationProperty("Regexes")]
            public SettingsCollection<IgnoreRegex> Regexes => this["Regexes"] as SettingsCollection<IgnoreRegex>;
            [ConfigurationProperty("Types")]
            public SettingsCollection<IgnoreType> Types => this["Types"] as SettingsCollection<IgnoreType>;

            /// <summary>
            /// Runs after deserialization, to populate <see cref="Settings.Ignore"/>.
            /// </summary>
            internal void Populate(Settings settings)
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
        }
        
        public class IgnoreRegex : SettingsCollectionElement
        {
            [ConfigurationProperty("name")]
            public override string Name => Get("name");
            [ConfigurationProperty("pattern", IsRequired = true)]
            public string Pattern => Get("pattern");
        }
        
        public class IgnoreType : SettingsCollectionElement
        {
            [ConfigurationProperty("name")]
            public override string Name => Get("name");
            [ConfigurationProperty("type", IsRequired = true)]
            public string Type => Get("type");
        }
    }
}