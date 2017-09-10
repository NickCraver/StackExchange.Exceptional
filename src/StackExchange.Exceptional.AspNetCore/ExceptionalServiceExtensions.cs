using Microsoft.Extensions.Configuration;
using StackExchange.Exceptional;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring Exceptional for MVC.
    /// </summary>
    public static class ExceptionalServiceExtensions
    {
        /// <summary>
        /// Adds Exceptional configuration for logging errors.
        /// </summary>
        /// <param name="services">The services collection to configure.</param>
        /// <param name="config">The config to bind to, e.g. Config.GetSection("Exceptional")</param>
        /// <param name="configureSettings">An <see cref="Action{ExceptionalSettings}"/> to configure options for Exceptional.</param>
        public static IServiceCollection AddExceptional(this IServiceCollection services, IConfiguration config, Action<ExceptionalSettings> configureSettings = null)
        {
            services.Configure<ExceptionalSettings>(config.Bind); // Custom extension
            return AddExceptional(services, configureSettings);
        }

        /// <summary>
        /// Adds Exceptional configuration for logging errors.
        /// </summary>
        /// <param name="services">The services collection to configure.</param>
        /// <param name="configureSettings">An <see cref="Action{ExceptionalSettings}"/> to configure options for Exceptional.</param>
        public static IServiceCollection AddExceptional(this IServiceCollection services, Action<ExceptionalSettings> configureSettings = null)
        {
            if (configureSettings != null)
            {
                services.Configure(configureSettings);
            }

            // When done configuring, set the background settings object for non-context logging.
            services.Configure<ExceptionalSettings>(Exceptional.Configure);

            return services;
        }
    }
}
