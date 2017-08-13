using System.Configuration;

namespace StackExchange.Exceptional
{
    internal partial class ConfigSettings
    {
        [ConfigurationProperty("LogFilters")]
        public LogFilterSettings LogFilters => this["LogFilters"] as LogFilterSettings;
        
        public class LogFilterSettings : ExceptionalElement
        {
            [ConfigurationProperty("Form")]
            public SettingsCollection<LogFilter> FormFilters => this["Form"] as SettingsCollection<LogFilter>;
            [ConfigurationProperty("Cookies")]
            public SettingsCollection<LogFilter> CookieFilters => this["Cookies"] as SettingsCollection<LogFilter>;
            
            internal void Populate(Settings settings)
            {
                var s = settings.LogFilters;
                foreach (LogFilter f in FormFilters)
                {
                    s.Form[f.Name] = f.ReplaceWith;
                }
                foreach (LogFilter c in CookieFilters)
                {
                    s.Cookie[c.Name] = c.ReplaceWith;
                }
            }
        }
        
        public class LogFilter : SettingsCollectionElement
        {
            [ConfigurationProperty("name", IsRequired = true)]
            public override string Name => Get("name");
            [ConfigurationProperty("replaceWith")]
            internal string ReplaceWith => Get("replaceWith");
        }
    }
}