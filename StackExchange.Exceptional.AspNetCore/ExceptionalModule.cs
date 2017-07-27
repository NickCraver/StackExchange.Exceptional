using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP module that catches and log exceptions from ASP.NET Applications.
    /// </summary>
    public class ExceptionalMiddleware
    {
        private RequestDelegate _next;

        static ExceptionalMiddleware() {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json");

            new ConfigSettings(builder.Build()).LoadSettings();
        }

        /// <summary>
        /// Gets the <see cref="ErrorStore"/> instance to which the module will log exceptions.
        /// </summary>        
        public virtual ErrorStore ErrorStore => ErrorStore.Default;

        public ExceptionalMiddleware(RequestDelegate next)
        {
            _next = next;
   
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                ex.Log(context);
            }
        }
    }

    public static class ExceptionalMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionalMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionalMiddleware>();
        }
    }
}