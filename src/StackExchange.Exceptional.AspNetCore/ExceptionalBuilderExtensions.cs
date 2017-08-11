using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Exceptional;
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
        public static IApplicationBuilder UseExceptional(this IApplicationBuilder builder, IConfiguration configuration, Action<Settings> configureSettings)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = configureSettings ?? throw new ArgumentNullException(nameof(configureSettings));

            var settings = Settings.Current;
            ConfigSettings.ConfigureSettings(configuration, settings);
            configureSettings(settings);
            return builder.UseMiddleware<ExceptionalMiddleware>(Options.Create(settings));
        }

        /// <summary>
        /// Adds middleware for capturing exceptions that occur during HTTP requests.
        /// This creates an in-memory only log using all default options except for those specified here.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <param name="applicationName">Application name for this error log.</param>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="applicationName"/> is <c>null</c>.</exception>
        public static IApplicationBuilder UseExceptional(this IApplicationBuilder builder, IConfiguration configuration, string applicationName)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            return builder.UseExceptional(configuration, settings =>
            {
                settings.ApplicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
            });
        }
    }
}