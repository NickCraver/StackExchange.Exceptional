using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class ErrorJsonHandlerMiddleware 
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();
        private readonly RequestDelegate _next;

        public ErrorJsonHandlerMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            DateTime? since = long.TryParse(context.Request.Query["since"], out long sinceLong)
                     ? new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(sinceLong)
                     : (DateTime?)null;

            var errors = ErrorStore.Default.GetAll();
            if (since.HasValue)
            {
                errors = errors.Where(error => error.CreationDate >= since).ToList();
            }
            await context.Response.WriteAsync(JsonConvert.SerializeObject(errors));
        }
    }

    public static class ErrorJsonHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorJsonHandlerMiddleware<T>(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorJsonHandlerMiddleware>(typeof(T));
        }
    }
}