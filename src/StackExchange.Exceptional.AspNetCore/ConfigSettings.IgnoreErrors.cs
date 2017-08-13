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
            public List<string> Regexes { get; set; } 

            public List<string> Types { get; set; } 

            internal void Populate(Settings settings)
            {
                var ignoreSettings = settings.Ignore;

                if (Regexes != null)
                {
                    foreach (var regex in Regexes)
                    {
                        if (regex.HasValue())
                        {
                            ignoreSettings.Regexes.Add(new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Singleline));
                        }
                    }
                }

                if (Types != null)
                {
                    foreach (var type in Types)
                    {
                        ignoreSettings.Types.Add(type);
                    }
                }
            }
        }
    }
}