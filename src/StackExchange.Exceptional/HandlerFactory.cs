using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
        static HandlerFactory() => Settings.LoadSettings();

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
            var resource = match.Success ? match.Groups["resource"].Value.ToLower(CultureInfo.InvariantCulture) : "";

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
                    switch (resource)
                    {
                        case KnownRoutes.Delete:
                            errorGuid = context.Request.Form["guid"] ?? "";
                            bool result = errorGuid.HasValue() && ErrorStore.Default.Delete(errorGuid.ToGuid());
                            return JSONPHandler(context, result) ?? new RedirectHandler(context.Request.Path.Replace("/delete", ""), false);

                        case KnownRoutes.DeleteAll:
                            bool delAllResult = ErrorStore.Default.DeleteAll();
                            return JSONPHandler(context, delAllResult) ?? new RedirectHandler(context.Request.Path.Replace("/delete-all", ""), false);

                        case KnownRoutes.DeleteList:
                            bool delListResult = ErrorStore.Default.DeleteList(getFormGuids());
                            return JsonResult(delListResult);

                        case KnownRoutes.Protect:
                            // send back a "true" or "false" - this will be handled in javascript
                            var pResult = ErrorStore.Default.Protect(context.Request.Form["guid"].ToGuid());
                            return JSONPHandler(context, pResult) ?? new ContentHandler(pResult.ToString(), "text/html");

                        case KnownRoutes.ProtectList:
                            bool protectListResult = ErrorStore.Default.ProtectList(getFormGuids());
                            return JsonResult(protectListResult);

                        default:
                            return new ContentHandler("Invalid POST Request", "text/html");
                    }
                case "GET":
                    errorGuid = context.Request.QueryString["guid"] ?? "";
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

        private ContentHandler JsonResult(bool result) =>
            new ContentHandler($@"{{""result"":{(result ? "true" : "false")}}}", "text/javascript");

        private ContentHandler Render(WebPage page) =>
            new ContentHandler(page.Render(), "text/html");

        private ContentHandler Render(Resources.ResourceCache cache) =>
            new ContentHandler(cache.Content, cache.MimeType);

        private IHttpHandler JSONPHandler(HttpContext context, bool result)
        {
            if (context.Request.QueryString["jsonp"].HasValue())
            {
                var response = $"{context.Request.QueryString["jsonp"]}({result.ToString().ToLower()});";
                return new ContentHandler(response, "text/javascript");
            }
            return null;
        }

        /// <summary>
        /// Enables the factory to reuse an existing handler instance.
        /// </summary>
        /// <param name="handler">The handler to release.</param>
        public virtual void ReleaseHandler(IHttpHandler handler) { }
    }
}
