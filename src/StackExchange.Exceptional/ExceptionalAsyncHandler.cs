using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Single handler for all module requests, async style.
    /// </summary>
    internal class ExceptionalAsyncHandler : HttpTaskAsyncHandler
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();
        private string Url { get; }

        public ExceptionalAsyncHandler(string url)
        {
            Url = url;
        }

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            void Content(string content, string mime = "text/html")
            {
                context.Response.ContentType = mime;
                context.Response.Write(content);
            }
            void JsonResult(bool result) => Content(@"{""result"":" + (result ? "true" : "false") + "}", "text/javascript");
            void Page(WebPage page) => Content(page.Render());
            void Resource(Resources.ResourceCache cache)
            {
                context.Response.Cache.SetCacheability(HttpCacheability.Private);
                context.Response.Cache.SetMaxAge(TimeSpan.FromDays(1));
                Content(cache.Content, cache.MimeType);
            }
            string TrimEnd(string s, string value) => s.EndsWith(value) ? s.Remove(s.LastIndexOf(value, StringComparison.Ordinal)) : s;

            // In MVC requests, PathInfo isn't set - determine via Path..
            // e.g. "/errors/info" or "/errors/"
            var match = Regex.Match(context.Request.Path, @"/?(?<resource>[\w\-\.]+)/?$");
            var resource = match.Success ? match.Groups["resource"].Value.ToLower(CultureInfo.InvariantCulture) : string.Empty;

            IEnumerable<Guid> GetFormGuids()
            {
                try
                {
                    return context.Request.Form["ids"]?.Split(',').Select(Guid.Parse) ?? Enumerable.Empty<Guid>();
                }
                catch { /* fall through */ }
                return Enumerable.Empty<Guid>();
            }

            var settings = Exceptional.Settings;
            var store = settings.DefaultStore;
            string errorGuid;

            switch (context.Request.HttpMethod)
            {
                case "POST":
                    errorGuid = context.Request.Form["guid"] ?? string.Empty;
                    switch (resource)
                    {
                        case KnownRoutes.Delete:
                            JsonResult(await store.DeleteAsync(errorGuid.ToGuid()).ConfigureAwait(false));
                            return;
                        case KnownRoutes.DeleteAll:
                            JsonResult(await store.DeleteAllAsync().ConfigureAwait(false));
                            return;
                        case KnownRoutes.DeleteList:
                            JsonResult(await store.DeleteAsync(GetFormGuids()).ConfigureAwait(false));
                            return;
                        case KnownRoutes.Protect:
                            JsonResult(await store.ProtectAsync(errorGuid.ToGuid()).ConfigureAwait(false));
                            return;
                        case KnownRoutes.ProtectList:
                            JsonResult(await store.ProtectAsync(GetFormGuids()).ConfigureAwait(false));
                            return;
                        default:
                            Content("Invalid POST Request");
                            return;
                    }
                case "GET":
                    errorGuid = context.Request.QueryString["guid"] ?? string.Empty;
                    switch (resource)
                    {
                        case KnownRoutes.Info:
                            var guid = errorGuid.ToGuid();
                            var error = errorGuid.HasValue() ? await store.GetAsync(guid).ConfigureAwait(false) : null;
                            Page(new ErrorDetailPage(error, settings, store, TrimEnd(context.Request.Path, "/info"), guid));
                            return;
                        case KnownRoutes.Json:
                            context.Response.ContentType = "application/json";
                            DateTime? since = long.TryParse(context.Request["since"], out long sinceLong)
                                     ? new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(sinceLong)
                                     : (DateTime?)null;

                            var errors = await store.GetAllAsync().ConfigureAwait(false);
                            if (since.HasValue)
                            {
                                errors = errors.Where(e => e.CreationDate >= since).ToList();
                            }
                            serializer.Serialize(context.Response.Output, errors);
                            return;
                        case KnownRoutes.Css:
                            Resource(Resources.BundleCss);
                            return;
                        case KnownRoutes.Js:
                            Resource(Resources.BundleJs);
                            return;
                        case KnownRoutes.Test:
                            throw new Exception("This is a test. Please disregard. If this were a real emergency, it'd have a different message.");
                        default:
                            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                            context.Response.Cache.SetNoStore();
                            string actualUrl = Uri.TryCreate(Url, UriKind.RelativeOrAbsolute, out var urlResult) && urlResult.IsAbsoluteUri ? urlResult.AbsolutePath : Url;
                            Page(new ErrorListPage(store, settings, actualUrl, await store.GetAllAsync().ConfigureAwait(false)));
                            return;
                    }
                default:
                    Content("Unsupported request method: " + context.Request.HttpMethod);
                    return;
            }
        }
    }
}
