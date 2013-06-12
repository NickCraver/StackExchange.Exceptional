using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using StackExchange.Exceptional.Handlers;
using StackExchange.Exceptional.Pages;
using StackExchange.Exceptional.Extensions;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP handler that chooses the correct handler/view based on the request.
    /// </summary>
    public class HandlerFactory : IHttpHandlerFactory
    {
        /// <summary>
        /// Gets the HttpHandler for executing the request, used to proxy requests through here (e.g. MVC) or by the HttpModule directly
        /// </summary>
        /// <param name="context">The HTTPContext for the request</param>
        /// <param name="requestType">The type of request, GET/POST</param>
        /// <param name="url">The URL of the request</param>
        /// <param name="pathTranslated">The translated path of the request</param>
        /// <returns>The HTTPHandler that can execute the request</returns>
        public virtual IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            // In MVC requests, PathInfo isn't set - determine via Path..
            // e.g. "/admin/errors/info" or "/admin/errors/"
            var match = Regex.Match(context.Request.Path, @"/?(?<resource>[\w\-\.]+)/?$");
            var resource = match.Success ? match.Groups["resource"].Value : "";

            Func<IEnumerable<Guid>> getFormGuids = () =>
                {
                    var idsStr = context.Request.Form["ids"];
                    try { if (idsStr.HasValue()) return idsStr.Split(',').Select(Guid.Parse); }
                    catch { return Enumerable.Empty<Guid>(); }
                    return Enumerable.Empty<Guid>();
                };

            switch(context.Request.HttpMethod)
            {
                // The chrome team, in their infinite wisdom, started pre-fetching URLs, making /delete-all being a GET a PITA
                case "POST":
                    switch (resource.ToLower(CultureInfo.InvariantCulture))
                    {
                        case "delete":
                            string errorGuid = context.Request.Form["guid"] ?? "";
                            bool result = false;
                            if (errorGuid.HasValue())
                            {
                                result = ErrorStore.Default.Delete(errorGuid.ToGuid());
                            }

                            return JSONPHandler(context, result) ?? new RedirectHandler(context.Request.Path.Replace("/delete", ""), false);

                        case "delete-all":
                            bool delAllResult = ErrorStore.Default.DeleteAll();
                            return JSONPHandler(context, delAllResult) ?? new RedirectHandler(context.Request.Path.Replace("/delete-all", ""), false);

                        case "delete-list":
                            bool delListResult = ErrorStore.Default.DeleteList(getFormGuids());
                            return new ContentHandler(new { result = delListResult }.ToJson(), "text/javascript");

                        case "protect":
                            // send back a "true" or "false" - this will be handled in javascript
                            var pResult = ErrorStore.Default.Protect(context.Request.Form["guid"].ToGuid());
                            return JSONPHandler(context, pResult) ?? new ContentHandler(pResult.ToString(), "text/html");

                        case "protect-list":
                            bool protectListResult = ErrorStore.Default.ProtectList(getFormGuids());
                            return new ContentHandler(new { result = protectListResult }.ToJson(), "text/javascript");

                        default:
                            return new ContentHandler("Invalid POST Request", "text/html");
                    }
                case "GET":
                    switch (resource.ToLower(CultureInfo.InvariantCulture))
                    {
                        case "delete":
                            string errorGuid = context.Request.QueryString["guid"] ?? "";
                            if (errorGuid.HasValue())
                            {
                                ErrorStore.Default.Delete(errorGuid.ToGuid());
                            }
                            return new RedirectHandler(context.Request.Path.Replace("/delete", ""), true);

                        case "info":
                            return new ErrorInfo { Guid = context.Request.QueryString["guid"].ToGuid() };

                        case "json":
                            return new ErrorJsonHandler();

                        case "css":
                            return new ResourceHandler("Styles.css", "text/css");

                        case "js":
                            return new ResourceHandler("Scripts.js", "text/javascript");

                        case "html5shiv":
                            return new ResourceHandler("html5shiv.js", "text/javascript");

                        case "top-bg.png":
                            return new ResourceHandler("top-bg.png", "image/png");

                        case "loading.gif":
                            return new ResourceHandler("loading.gif", "image/gif");

                        case "top-bg-fail.png":
                            return new ResourceHandler("top-bg-fail.png", "image/png");

                        case "test":
                            throw new Exception("This is a test. Please disregard. If this were a real emergency, it'd have a different message.");

                        default:
                            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                            context.Response.Cache.SetNoStore();
                            return new ErrorList();
                    }
                default:
                    return new ContentHandler("Unsupported request method: " + context.Request.HttpMethod, "text/html");
            }
        }

        private IHttpHandler JSONPHandler(HttpContext context, bool result)
        {
            if (context.Request.QueryString["jsonp"].HasValue())
            {
                var response = string.Format("{0}({1});", context.Request.QueryString["jsonp"], result.ToString().ToLower());
                return new ContentHandler(response, "text/javascript");
            }
            return null;
        }

        /// <summary>
        /// Enables the factory to reuse an existing handler instance.
        /// </summary>
        public virtual void ReleaseHandler(IHttpHandler handler) { }
    }
}
