using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Exceptional;
using StackExchange.Exceptional.Internal;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the Exceptional middleware.
    /// </summary>
    public static class ExceptionalBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for capturing exceptions that occur during HTTP requests.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <param name="configureSettings">The action configuring Exceptional settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configureSettings"/> is <c>null</c>.</exception>
        public static IApplicationBuilder UseExceptional(this IApplicationBuilder builder, Action<Settings> configureSettings)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = configureSettings ?? throw new ArgumentNullException(nameof(configureSettings));
            
            var settings = Settings.Current;
            configureSettings(settings);
            return builder.UseMiddleware<ExceptionalMiddleware>(Options.Create(settings));
        }

        /// <summary>
        /// Adds middleware for capturing exceptions that occur during HTTP requests.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <param name="config">The config to bind to, e.g. Config.GetSection("Exceptional")</param>
        /// <param name="configureSettings">The action configuring Exceptional settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configureSettings"/> is <c>null</c>.</exception>
        public static IApplicationBuilder UseExceptional(this IApplicationBuilder builder, IConfiguration config, Action<Settings> configureSettings = null)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = configureSettings ?? throw new ArgumentNullException(nameof(configureSettings));

            var settings = Settings.Current;
            var configSettings = new ConfigSettings(config);
            configSettings.Populate(settings);
            configureSettings?.Invoke(settings);
            return builder.UseMiddleware<ExceptionalMiddleware>(Options.Create(settings));
        }
    }
}