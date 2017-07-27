using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Builder;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP handler that chooses the correct handler/view based on the request.
    /// </summary>
    public class HandlerFactoryMiddleware 
    {
        private RequestDelegate _next;

        static HandlerFactoryMiddleware() => ConfigSettings.LoadSettings();

        public HandlerFactoryMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var handler = new ExceptionalAsyncHandler(context.Request.GetDisplayUrl());
            await handler.ProcessRequestAsync(context);
            await _next.Invoke(context);
        }
    }

    public static class HandlerFactoryMiddlewareExtensions
    {
        public static IApplicationBuilder UseHandlerFactoryMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HandlerFactoryMiddleware>();
        }
    }
}
