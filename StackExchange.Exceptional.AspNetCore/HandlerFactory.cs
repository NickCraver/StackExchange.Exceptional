using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using StackExchange.Exceptional.Handlers;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Pages;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Builder;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP handler that chooses the correct handler/view based on the request.
    /// </summary>
    public class HandlerFactoryMiddleware 
    {
        private RequestDelegate _next;

        static HandlerFactoryMiddleware()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json");
        }

        public HandlerFactoryMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Gets the HttpHandler for executing the request, used to proxy requests through here (e.g. MVC) or by the HttpModule directly.
        /// </summary>
        /// <param name="context">The HTTPContext for the request.</param>
        /// <param name="requestType">The type of request, GET/POST.</param>
        /// <param name="url">The URL of the request.</param>
        /// <param name="pathTranslated">The translated path of the request.</param>
        /// <returns>The HTTPHandler that can execute the request.</returns>
        public async Task Invoke(HttpContext context)
        {
            // In MVC requests, PathInfo isn't set - determine via Path..
            // e.g. "/admin/errors/info" or "/admin/errors/"
            var match = Regex.Match(context.Request.Path, @"/?(?<resource>[\w\-\.]+)/?$");
            var resource = match.Success ? match.Groups["resource"].Value.ToLower(CultureInfo.InvariantCulture) : "";

            Func<IEnumerable<Guid>> getFormGuids = () =>
                {
                    var idsStr = context.Request.Form["ids"];
                    try { if (idsStr.Count > 0) return idsStr[0].Split(',').Select(Guid.Parse); }
                    catch { return Enumerable.Empty<Guid>(); }
                    return Enumerable.Empty<Guid>();
                };

            string errorGuid;

            ContentHandlerMiddleware contentHandler = null;

            switch (context.Request.Method)
            {
                case "POST":
                    switch (resource)
                    {
                        case KnownRoutes.Delete:
                            errorGuid = context.Request.Form["guid"].ToString();
                            bool result = errorGuid.HasValue() && ErrorStore.Default.Delete(errorGuid.ToGuid());
                            contentHandler = JSONPHandler(context, result);
                            if (contentHandler == null)
                                await new RedirectHandlerMiddleware(_next, context.Request.Path.ToString().Replace("/delete", ""), false).Invoke(context);
                            else
                                await contentHandler.Invoke(context);
                            break;

                        case KnownRoutes.DeleteAll:
                            bool delAllResult = ErrorStore.Default.DeleteAll();
                            contentHandler = JSONPHandler(context, delAllResult);
                            if (contentHandler == null)
                                await new RedirectHandlerMiddleware(_next, context.Request.Path.ToString().Replace("/delete-all", ""), false).Invoke(context);
                            else
                                await contentHandler.Invoke(context);
                            break;

                        case KnownRoutes.DeleteList:
                            bool delListResult = ErrorStore.Default.Delete(getFormGuids());
                            await JsonResult(delListResult).Invoke(context);
                            break;

                        case KnownRoutes.Protect:
                            // send back a "true" or "false" - this will be handled in JavaScript
                            var pResult = ErrorStore.Default.Protect(context.Request.Form["guid"].ToString().ToGuid());
                            contentHandler = JSONPHandler(context, pResult);
                            if (contentHandler == null)
                                await new ContentHandlerMiddleware(_next, pResult.ToString(), "text/html").Invoke(context);
                            else
                                await contentHandler.Invoke(context);
                            break;
                        case KnownRoutes.ProtectList:
                            bool protectListResult = ErrorStore.Default.Protect(getFormGuids());
                            await JsonResult(protectListResult).Invoke(context);
                            break;

                        default:
                            await new ContentHandlerMiddleware(_next, "Invalid POST Request", "text/html").Invoke(context);
                            break;
                    }
                    break;
                case "GET":
                    errorGuid = context.Request.Query["guid"].ToString();
                    switch (resource)
                    {
                        case KnownRoutes.Info:
                            var guid = errorGuid.ToGuid();
                            var error = errorGuid.HasValue() ? ErrorStore.Default.Get(guid) : null;
                            await Render(new ErrorDetailPage(error, ErrorStore.Default, TrimEnd(context.Request.Path, "/info"), guid)).Invoke(context);
                            break;

                        case KnownRoutes.Json:
                            await new ErrorJsonHandlerMiddleware(_next).Invoke(context);
                            break;

                        case KnownRoutes.Css:
                            await Render(Resources.BundleCss).Invoke(context);
                            break;

                        case KnownRoutes.Js:
                            await Render(Resources.BundleJs).Invoke(context);
                            break;

                        case KnownRoutes.Test:
                            throw new Exception("This is a test. Please disregard. If this were a real emergency, it'd have a different message.");

                        default:
                            context.Response.GetTypedHeaders().CacheControl.NoCache = true;
                            context.Response.GetTypedHeaders().CacheControl.NoStore = true;
                            await Render(new ErrorListPage(ErrorStore.Default, context.Request.Path)).Invoke(context);
                            break;
                    }
                    break;
                default:
                    await new ContentHandlerMiddleware(_next, "Unsupported request method: " + context.Request.Method, "text/html").Invoke(context);
                    break;
            }
        }

        private string TrimEnd(string s, string value) =>
            s.EndsWith(value) ? s.Remove(s.LastIndexOf(value, StringComparison.Ordinal)) : s;

        private ContentHandlerMiddleware JsonResult(bool result) =>
            new ContentHandlerMiddleware(_next, $@"{{""result"":{(result ? "true" : "false")}}}", "text/javascript");

        private ContentHandlerMiddleware Render(WebPage page) =>
            new ContentHandlerMiddleware(_next, page.Render(), "text/html");

        private ContentHandlerMiddleware Render(Resources.ResourceCache cache) =>
            new ContentHandlerMiddleware(_next, cache.Content, cache.MimeType);

        private ContentHandlerMiddleware JSONPHandler(HttpContext context, bool result)
        {
            if (context.Request.Query["jsonp"].Count > 0)
            {
                var response = $"{context.Request.Query["jsonp"]}({result.ToString().ToLower()});";
                return new ContentHandlerMiddleware(_next, response, "text/javascript");
            }
            return null;
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
