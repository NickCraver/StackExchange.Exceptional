using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP module that catches and log exceptions from ASP.NET Applications.
    /// </summary>
    public class ExceptionalMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IHostingEnvironment _env;
        private readonly IOptions<ExceptionalSettings> _settings;

        /// <summary>
        /// Creates a new instance of <see cref="ExceptionalMiddleware"/>
        /// </summary>
        /// <param name="next">The next delegate in the chain to execute.</param>
        /// <param name="settings">The <see cref="ExceptionalSettings"/> wrapper that contains current settings.</param>
        /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/> to get pathing data from.</param>
        /// <param name="loggerFactory">The logger factor to record exceptions to.</param>
        public ExceptionalMiddleware(
            RequestDelegate next,
            IOptions<ExceptionalSettings> settings,
            IHostingEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _env = hostingEnvironment;
            _logger = loggerFactory.CreateLogger<ExceptionalMiddleware>();
            _settings = settings;

            // If an ApplicationName isn't provided, default to IHostingEnvironment.ApplicationName
            if (!_settings.Value.Store.ApplicationName.HasValue())
            {
                _settings.Value.Store.ApplicationName = _env.ApplicationName;
            }
        }

        /// <summary>
        /// Executes the Exceptional-wrapped middleware.
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/>.</param>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "An unhandled exception has occurred, logging to Exceptional");
                var error = await ex.LogAsync(context).ConfigureAwait(false);

                // If options say to do so, show the exception page to the user
                if (_settings.Value.UseExceptionalPageOnThrow && error != null)
                {
                    var response = context.Response;
                    if (context.Response.HasStarted)
                    {
                        _logger.LogWarning("The response has already started, the Exceptional error page will not be displayed.");
                        throw;
                    }

                    try
                    {
                        response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = null;
                        response.Headers.Clear();
                        if (response.Body.CanSeek)
                        {
                            response.Body.SetLength(0);
                        }
                        response.StatusCode = 500;

                        var page = new ErrorDetailPage(error, _settings.Value, _settings.Value.DefaultStore, "", error.GUID)
                        {
                            HeaderTitle = "An error was thrown during this request.",
                            PageTitle = "An error was thrown during this request.",
                            InlineCSS = true,
                            IncludeJS = false,
                            ShowActionLinks = false
                        };
                        response.ContentType = "text/html";
                        await response.WriteAsync(page.Render()).ConfigureAwait(false);

                        return;
                    }
                    catch (Exception pex)
                    {
                        _logger.LogError(0, pex, "An exception was thrown attempting to display the Exceptional page.");
                    }
                }
                throw;
            }
        }

        /// <summary>
        /// Convenience method for handling a request, for usage in your routing, MVC, etc. See example.
        /// </summary>
        /// <example>
        /// <code>
        /// [Route("/path/my-route")]
        /// public async Task Exceptions() => await ExceptionalMiddleware.HandleRequestAsync(HttpContext);
        /// </code>
        /// </example>
        /// <param name="context">The context to process, usually HttpContext from a controller.</param>
        /// <returns>A task to await.</returns>
        public static async Task HandleRequestAsync(HttpContext context)
        {
            async Task Content(string content, string mime = "text/html")
            {
                context.Response.ContentType = mime;
                await context.Response.WriteAsync(content).ConfigureAwait(false);
            }
            Task JsonResult(bool result) => Content(@"{""result"":" + (result ? "true" : "false") + "}", "text/javascript");
            Task Page(WebPage page) => Content(page.Render());
            Task Resource(Resources.ResourceCache cache)
            {
                context.Response.Headers["Cache-Control"] = "private, max-age=86400";
                return Content(cache.Content, cache.MimeType);
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
                    return context.Request.Form["ids"].Select(Guid.Parse) ?? Enumerable.Empty<Guid>();
                }
                catch
                {
                    return Enumerable.Empty<Guid>();
                }
            }

            var settings = context.RequestServices.GetRequiredService<IOptions<ExceptionalSettings>>().Value;
            var store = settings.DefaultStore;
            string errorGuid;

            switch (context.Request.Method)
            {
                case "POST":
                    errorGuid = context.Request.HasFormContentType ? context.Request.Form["guid"].ToString() : string.Empty;
                    switch (resource)
                    {
                        case KnownRoutes.Delete:
                            await JsonResult(await store.DeleteAsync(errorGuid.ToGuid()).ConfigureAwait(false)).ConfigureAwait(false);
                            return;
                        case KnownRoutes.DeleteAll:
                            await JsonResult(await store.DeleteAllAsync().ConfigureAwait(false)).ConfigureAwait(false);
                            return;
                        case KnownRoutes.DeleteList:
                            await JsonResult(await store.DeleteAsync(GetFormGuids()).ConfigureAwait(false)).ConfigureAwait(false);
                            return;
                        case KnownRoutes.Protect:
                            await JsonResult(await store.ProtectAsync(errorGuid.ToGuid()).ConfigureAwait(false)).ConfigureAwait(false);
                            return;
                        case KnownRoutes.ProtectList:
                            await JsonResult(await store.ProtectAsync(GetFormGuids()).ConfigureAwait(false)).ConfigureAwait(false);
                            return;
                        default:
                            await Content("Invalid POST Request").ConfigureAwait(false);
                            return;
                    }
                case "GET":
                    errorGuid = context.Request.Query["guid"].ToString() ?? string.Empty;
                    switch (resource)
                    {
                        case KnownRoutes.Info:
                            var guid = errorGuid.ToGuid();
                            var error = errorGuid.HasValue() ? await store.GetAsync(guid).ConfigureAwait(false) : null;
                            await Page(new ErrorDetailPage(error, settings, store, TrimEnd($"{context.Request.PathBase}{context.Request.Path}", "/info"), guid)).ConfigureAwait(false);
                            return;
                        case KnownRoutes.Json:
                            context.Response.ContentType = "application/json";
                            DateTime? since = long.TryParse(context.Request.Headers["since"], out long sinceLong)
                                     ? new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(sinceLong)
                                     : (DateTime?)null;

                            var errors = await store.GetAllAsync().ConfigureAwait(false);
                            if (since.HasValue)
                            {
                                errors = errors.Where(e => e.CreationDate >= since).ToList();
                            }
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(errors)).ConfigureAwait(false);
                            return;
                        case KnownRoutes.Css:
                            await Resource(Resources.BundleCss).ConfigureAwait(false);
                            return;
                        case KnownRoutes.Js:
                            await Resource(Resources.BundleJs).ConfigureAwait(false);
                            return;
                        case KnownRoutes.Test:
                            throw new Exception("This is a test. Please disregard. If this were a real emergency, it'd have a different message.");
                        default:
                            context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                            await Page(new ErrorListPage(store, settings, $"{context.Request.PathBase}{context.Request.Path}", await store.GetAllAsync().ConfigureAwait(false))).ConfigureAwait(false);
                            return;
                    }
                default:
                    await Content("Unsupported request method: " + context.Request.Method).ConfigureAwait(false);
                    return;
            }
        }
    }
}
