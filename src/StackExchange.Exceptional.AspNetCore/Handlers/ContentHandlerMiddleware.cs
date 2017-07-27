using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class ContentHandlerMiddleware 
    {
        private readonly RequestDelegate _next;
        private readonly string _content;
        private readonly string _contentType;

        public ContentHandlerMiddleware(RequestDelegate next, string message, string contentType)
        {
            _next = next;
            _content = message;
            _contentType = contentType;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.ContentType = _contentType;
            await context.Response.WriteAsync(_content);
        }
    }

    public static class ContentMiddlewareExtensions
    {
        public static IApplicationBuilder UseContentHandlerMiddleware<T>(this IApplicationBuilder builder, string message, string contentType)
        {
            return builder.UseMiddleware<ContentHandlerMiddleware>(typeof(T), message, contentType);
        }
    }
}