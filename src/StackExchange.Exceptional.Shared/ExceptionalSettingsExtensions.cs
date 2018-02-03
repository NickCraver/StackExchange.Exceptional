using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Notifiers;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Extension methods for <see cref="ExceptionalSettingsBase"/>.
    /// </summary>
    public static class ExceptionalSettingsExtensions
    {
        /// <summary>
        /// Binds an <see cref="IConfiguration"/> to an <typeparamref name="T"/> object.
        /// This happens with a normal .Bind() followed by the complex type mappings manually.
        /// </summary>
        /// <typeparam name="T">The specific type of <see cref="ExceptionalSettingsBase"/> to bind to.</typeparam>
        /// <param name="config">The <see cref="IConfigurationSection"/> to bind.</param>
        /// <param name="settings">The <typeparamref name="T"/> to bind to.</param>
        public static void Bind<T>(this IConfiguration config, T settings) where T : ExceptionalSettingsBase
        {
            // Bind the simple types (almost everything)
            ConfigurationBinder.Bind(config, settings); // because we overrode .Bind() here

            // Now, explicitly bind the complex types
            var dataIncludePattern = config.GetValue<string>(nameof(ExceptionalSettingsBase.DataIncludeRegex));
            if (!string.IsNullOrEmpty(dataIncludePattern))
            {
                settings.DataIncludeRegex = new Regex(dataIncludePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            var ignoreRegexes = config.GetSection(nameof(ExceptionalSettingsBase.Ignore))
                                .GetSection(nameof(ExceptionalSettingsBase.IgnoreSettings.Regexes))
                                .AsEnumerable();
            foreach (var ir in ignoreRegexes)
            {
                if (ir.Value != null)
                {
                    settings.Ignore.Regexes.Add(new Regex(ir.Value, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline));
                }
            }
            // If email is configured, hook it up
            if (settings.Email.ToAddress.HasValue())
            {
                settings.Register(new EmailNotifier(settings.Email));
            }
        }
    }
}
