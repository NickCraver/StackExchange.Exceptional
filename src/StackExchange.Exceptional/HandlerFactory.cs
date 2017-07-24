using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using StackExchange.Exceptional.Handlers;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Pages;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP handler that chooses the correct handler/view based on the request.
    /// </summary>
    public class HandlerFactory : IHttpHandlerFactory
    {
        static HandlerFactory() => ConfigSettings.LoadSettings();

        /// <summary>
        /// Gets the HttpHandler for executing the request, used to proxy requests through here (e.g. MVC) or by the HttpModule directly.
        /// </summary>
        /// <param name="context">The HTTPContext for the request.</param>
        /// <param name="requestType">The type of request, GET/POST.</param>
        /// <param name="url">The URL of the request.</param>
        /// <param name="pathTranslated">The translated path of the request.</param>
        /// <returns>The HTTPHandler that can execute the request.</returns>
        public virtual IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            // In MVC requests, PathInfo isn't set - determine via Path..
            // e.g. "/admin/errors/info" or "/admin/errors/"
            var match = Regex.Match(context.Request.Path, @"/?(?<resource>[\w\-\.]+)/?$");
            var resource = match.Success ? match.Groups["resource"].Value.ToLower(CultureInfo.InvariantCulture) : string.Empty;

            Func<IEnumerable<Guid>> getFormGuids = () =>
                {
                    var idsStr = context.Request.Form["ids"];
                    try { if (idsStr.HasValue()) return idsStr.Split(',').Select(Guid.Parse); }
                    catch { return Enumerable.Empty<Guid>(); }
                    return Enumerable.Empty<Guid>();
                };

            string errorGuid;

            switch (context.Request.HttpMethod)
            {
                case "POST":
                    errorGuid = context.Request.Form["guid"] ?? string.Empty;
                    switch (resource)
                    {
                        case KnownRoutes.Delete:
                            return JsonResult(ErrorStore.Default.DeleteAsync(errorGuid.ToGuid()));

                        case KnownRoutes.DeleteAll:
                            return JsonResult(ErrorStore.Default.DeleteAllAsync());

                        case KnownRoutes.DeleteList:
                            return JsonResult(ErrorStore.Default.DeleteAsync(getFormGuids()));

                        case KnownRoutes.Protect:
                            return JsonResult(ErrorStore.Default.ProtectAsync(errorGuid.ToGuid()));

                        case KnownRoutes.ProtectList:
                            return JsonResult(ErrorStore.Default.ProtectAsync(getFormGuids()));

                        default:
                            return new ContentHandler("Invalid POST Request", "text/html");
                    }
                case "GET":
                    errorGuid = context.Request.QueryString["guid"] ?? string.Empty;
                    switch (resource)
                    {
                        case KnownRoutes.Info:
                            var guid = errorGuid.ToGuid();
                            var error = errorGuid.HasValue() ? ErrorStore.Default.Get(guid) : null;
                            return Render(new ErrorDetailPage(error, ErrorStore.Default, TrimEnd(context.Request.Path, "/info"), guid));

                        case KnownRoutes.Json:
                            return new ErrorJsonHandler();

                        case KnownRoutes.Css:
                            return Render(Resources.BundleCss);

                        case KnownRoutes.Js:
                            return Render(Resources.BundleJs);

                        case KnownRoutes.Test:
                            throw new Exception("This is a test. Please disregard. If this were a real emergency, it'd have a different message.");

                        default:
                            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                            context.Response.Cache.SetNoStore();
                            return Render(new ErrorListPage(ErrorStore.Default, url));
                    }
                default:
                    return new ContentHandler("Unsupported request method: " + context.Request.HttpMethod, "text/html");
            }
        }

        private string TrimEnd(string s, string value) =>
            s.EndsWith(value) ? s.Remove(s.LastIndexOf(value, StringComparison.Ordinal)) : s;

        private JsonAsyncHandler JsonResult(Task<bool> task) => new JsonAsyncHandler(task);

        private ContentHandler Render(WebPage page) => new ContentHandler(page.Render(), "text/html");

        private ContentHandler Render(Resources.ResourceCache cache) => new ContentHandler(cache.Content, cache.MimeType);

        private RedirectHandler Redirect(string url) => new RedirectHandler(url);

        /// <summary>
        /// Enables the factory to reuse an existing handler instance.
        /// </summary>
        /// <param name="handler">The handler to release.</param>
        public virtual void ReleaseHandler(IHttpHandler handler) { }
    }

    internal class JsonAsyncHandler : HttpTaskAsyncHandler
    {
        private Task<bool> _task { get; }
        public JsonAsyncHandler(Task<bool> task)
        {
            _task = task;
        }

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            var result = await _task.ConfigureAwait(false);
            context.Response.ContentType = "text/javascript";
            context.Response.Write($@"{{""result"":{(result ? "true" : "false")}}}");
        }
    }
}
