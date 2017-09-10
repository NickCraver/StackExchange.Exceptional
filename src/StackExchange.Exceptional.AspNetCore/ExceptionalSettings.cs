using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using StackExchange.Exceptional.Internal;
using System;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// ASP.NET Core settings for Exceptional error logging.
    /// </summary>
    public class ExceptionalSettings : ExceptionalSettingsBase
    {
        /// <summary>
        /// Whether to show the Exceptional page on throw, instead of the built-in .UseDeveloperExceptionPage()
        /// </summary>
        public bool UseExceptionalPageOnThrow { get; set; }

        /// <summary>
        /// Method of getting the IP address for the error, defaults to retrieving it from server variables.
        /// but may need to be replaced in special multi-proxy situations.
        /// </summary>
        public Func<HttpContext, string> GetIPAddress { get; set; } = context => context.Connection.RemoteIpAddress.ToString();
    }

    /// <summary>
    /// Extension methods for <see cref="ExceptionalSettings"/>.
    /// </summary>
    public static class ExceptionalSettingsExtensions
    {
        /// <summary>
        /// Binds an <see cref="IConfiguration"/> to an <see cref="ExceptionalSettings"/> object.
        /// This happens with a normal .Bind() followed by the complex type mappings manually.
        /// </summary>
        /// <param name="config">The <see cref="IConfigurationSection"/> to bind.</param>
        /// <param name="settings">The <see cref="ExceptionalSettings"/> to bind to.</param>
        public static void Bind(this IConfiguration config, ExceptionalSettings settings)
        {
            // Bind the simple types (almost everything)
            ConfigurationBinder.Bind(config, settings); // because we overrode .Bind() here

            // Now, explicitly bind the complex types
            var dataIncludePattern = config.GetValue<string>(nameof(ExceptionalSettings.DataIncludeRegex));
            if (!string.IsNullOrEmpty(dataIncludePattern))
            {
                settings.DataIncludeRegex = new Regex(dataIncludePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            var ignoreRegexes = config.GetSection(nameof(ExceptionalSettings.Ignore))
                                .GetSection(nameof(ExceptionalSettingsBase.IgnoreSettings.Regexes))
                                .AsEnumerable();
            foreach (var ir in ignoreRegexes)
            {
                if (ir.Value != null)
                {
                    settings.Ignore.Regexes.Add(new Regex(ir.Value, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline));
                }
            }
        }
    }
}
