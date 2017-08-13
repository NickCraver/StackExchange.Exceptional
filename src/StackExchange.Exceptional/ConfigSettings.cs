using StackExchange.Exceptional.Internal;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// The Settings element for Exceptional's configuration.
    /// This is the legacy web.config settings, that only serve as an adapter to populate <see cref="Settings"/>.
    /// </summary>
    internal partial class ConfigSettings : ConfigurationSection
    {
        private static int _loaded;
        /// <summary>
        /// Trigger deserialization, which loads settings from the .config file.
        /// </summary>
        public static void LoadSettings()
        {
            if (Interlocked.CompareExchange(ref _loaded, 1, 0) == 0)
            {
                var config = ConfigurationManager.GetSection("Exceptional") as ConfigSettings;
                config.Populate(Settings.Current);
            }
        }
        
        [ConfigurationProperty("applicationName", IsRequired = true)]
        public string ApplicationName => this["applicationName"] as string;
        [ConfigurationProperty("dataIncludePattern")]
        public string DataIncludePattern => this["dataIncludePattern"] as string;

        internal void Populate(Settings settings)
        {
            settings.ApplicationName = ApplicationName;
            if (DataIncludePattern.HasValue())
            {
                settings.DataIncludeRegex = new Regex(DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
            }

            Email?.Populate(settings);
            ErrorStore?.Populate(settings);
            Ignore?.Populate(settings);
            LogFilters?.Populate(settings);
        }

        /// <summary>
        /// A collection of list types all with a Name attribute.
        /// </summary>
        /// <typeparam name="T">The type of collection, inherited from SettingsCollectionElement.</typeparam>
        public class SettingsCollection<T> : ConfigurationElementCollection where T : SettingsCollectionElement, new()
        {
            /// <summary>
            /// Accessor by key.
            /// </summary>
            /// <param name="key">The key to lookup.</param>
            public new T this[string key] => BaseGet(key) as T;

            /// <summary>
            /// Accessor by index.
            /// </summary>
            /// <param name="index">The index position to lookup.</param>
            public T this[int index] => BaseGet(index) as T;

            /// <summary>
            /// Default constructor for this element.
            /// </summary>
            protected override ConfigurationElement CreateNewElement() => new T();

            /// <summary>
            /// Default by-key fetch for this element.
            /// </summary>
            /// <param name="element">The element to get a key for.</param>
            protected override object GetElementKey(ConfigurationElement element) => element.ToString();
        }

        /// <summary>
        /// An element in a settings collection that has a Name property, a generic base for SettingsCollection collections.
        /// </summary>
        internal abstract class SettingsCollectionElement : ExceptionalElement
        {
            public override string ToString() => Name;
            public abstract string Name { get; }
        }

        internal class ExceptionalElement : ConfigurationElement
        {
            public string Get(string name) => this[name] as string;
            public bool? GetBool(string name) => (bool?)this[name];
            public int? GetInt(string name) => (int?)this[name];
        }
    }
}