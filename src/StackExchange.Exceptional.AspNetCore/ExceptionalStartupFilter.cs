using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StackExchange.Exceptional
{
    internal class ExceptionalStartupFilter : IStartupFilter
    {
        /// <summary>
        /// Configures exceptional early on, as to catch any errors that happen in Startup.Configure (or equivalent).
        /// </summary>
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            builder =>
            {
                // Touch settings so that we initialize early
                _ = builder.ApplicationServices.GetService<IOptions<ExceptionalSettings>>().Value;
                next(builder);
            };
    }
}
