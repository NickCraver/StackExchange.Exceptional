using StackExchange.Exceptional.Internal;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// The Settings element for Exceptional's configuration.
    /// This is the legacy web.config settings, that only serve as an adapter to populate <see cref="Settings"/>.
    /// </summary>
    internal partial class ConfigSettings : ConfigurationSection
    {
        private static ConfigSettings _current;
        /// <summary>
        /// Trigger deserialization, which loads settings from the .config file.
        /// </summary>
        public static void LoadSettings()
        {
            if (_current == null)
            {
                _current = ConfigurationManager.GetSection("Exceptional") as ConfigSettings;
            }
        }

        /// <summary>
        /// Application name to log with.
        /// </summary>
        [ConfigurationProperty("applicationName", IsRequired = true)]
        public string ApplicationName => this["applicationName"] as string;

        /// <summary>
        /// The regular expression pattern of data keys to include. 
        /// For example, "Redis.*" would include all keys that start with Redis.
        /// </summary>
        [ConfigurationProperty("dataIncludePattern")]
        public string DataIncludePattern => this["dataIncludePattern"] as string;

        /// <summary>
        /// Runs after deserialization, to populate <see cref="Settings"/>.
        /// </summary>
        protected override void PostDeserialize()
        {
            base.PostDeserialize();
            // Main settings
            Settings.Current.ApplicationName = ApplicationName;
            if (DataIncludePattern.HasValue())
            {
                Settings.Current.DataIncludeRegex = new Regex(DataIncludePattern, RegexOptions.Singleline | RegexOptions.Compiled);
            }
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

            /// <summary>
            /// Returns all the elements in this collection, type-cased out.
            /// </summary>
            public List<T> All => this.Cast<T>().ToList();
        }

        /// <summary>
        /// An element in a settings collection that has a Name property, a generic base for SettingsCollection collections.
        /// </summary>
        public abstract class SettingsCollectionElement : ConfigurationElement
        {
            /// <summary>
            /// String representation for this entry, the Name.
            /// </summary>
            public override string ToString() => Name;
            /// <summary>
            /// A unique name for this entry.
            /// </summary>
            public abstract string Name { get; }
        }
    }
}