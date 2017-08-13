using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Serialization;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        public LogFilterSettings LogFilters { get; set; } 

        public class LogFilterSettings
        {
            public List<LogFilter> Form { get; set; } = new List<LogFilter>();

            public List<LogFilter> Cookies { get; set; } = new List<LogFilter>();

            internal void Populate(Settings settings)
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

        public class LogFilter
        {
            public string Name { get; set; }

            public string ReplaceWith { get; set; }
        }
    }
}