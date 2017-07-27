using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class RedirectHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _url;
        private readonly bool _redirectIfAjax;

        public RedirectHandlerMiddleware(RequestDelegate next, string url, bool redirectIfAjax)
        {
            _next = next;
            _url = url;
            _redirectIfAjax = redirectIfAjax;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!_redirectIfAjax && context.Request.Headers["X-Requested-With"] == "XMLHttpRequest") return;
            context.Response.Redirect(_url);
        }
    }

    public static class RedirectHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseRedirectHandlerMiddleware<T>(this IApplicationBuilder builder, string url, bool redirectIfAjax)
        {
            return builder.UseMiddleware<RedirectHandlerMiddleware>(typeof(T), url, redirectIfAjax);
        }
    }
}