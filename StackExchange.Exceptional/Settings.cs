using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace StackExchange.Exceptional
{
    public partial class Settings : ConfigurationSection
    {
        private static readonly Settings _settings = ConfigurationManager.GetSection("Exceptional") as Settings;
        public static Settings Current { get { return _settings ?? new Settings(); } }

        [ConfigurationProperty("applicationName", IsRequired = true)]
        public string ApplicationName { get { return this["applicationName"] as string; } }
        
        public class SettingsCollection<T> : ConfigurationElementCollection where T : SettingsCollectionElement, new()
        {
            public new T this[string key]
            {
                get { return BaseGet(key) as T; }
            }
            public T this[int index]
            {
                get { return BaseGet(index) as T; }
            }
            protected override ConfigurationElement CreateNewElement()
            {
                return new T();
            }
            protected override object GetElementKey(ConfigurationElement element)
            {
                return element.ToString();
            }
            public List<T> All
            {
                get { return this.Cast<T>().ToList(); }
            }
        }

        public abstract class SettingsCollectionElement : ConfigurationElement
        {
            public override string ToString() { return Name; }
            public abstract string Name { get; }
        }
    }
}