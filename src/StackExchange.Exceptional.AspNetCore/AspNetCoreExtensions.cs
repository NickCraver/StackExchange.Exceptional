using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Extensions methods for logging an <see cref="Exception"/>.
    /// </summary>
    public static class AspNetCoreExtensions
    {
        /// <summary>
        /// Logs an exception to the configured error store, or the in-memory default store if none is configured.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="context">The HTTPContext to record variables from.  If this isn't a web request, pass <see langword="null" /> in here.</param>
        /// <param name="category">The category to associate with this exception.</param>
        /// <param name="rollupPerServer">Whether to log up per-server, e.g. errors are only duplicates if they have same stack on the same machine.</param>
        /// <param name="customData">Any custom data to store with the exception like UserId, etc...this will be rendered as JSON in the error view for script use.</param>
        /// <param name="applicationName">If specified, the application name to log with, if not specified the name in <see cref="ErrorStoreSettings.ApplicationName"/> is used.</param>
        /// <returns>The Error created, if one was created and logged, null if nothing was logged.</returns>
        /// <remarks>
        /// When dealing with a non web requests, pass <see langword="null" /> in for context.  
        /// It shouldn't be forgotten for most web application usages, so it's not an optional parameter.
        /// </remarks>
        public static Error Log(
            this Exception ex,
            HttpContext context,
            string category = null,
            bool rollupPerServer = false,
            Dictionary<string, string> customData = null,
            string applicationName = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (Statics.IsLoggingEnabled)
            {
                try
                {
                    var settings = context.RequestServices.GetRequiredService<IOptions<ExceptionalSettings>>().Value;
                    // If we should be ignoring this exception, skip it entirely.
                    // Otherwise create the error itself, populating CustomData with what was passed-in.
                    var error = ex.GetErrorIfNotIgnored(settings, category, applicationName, rollupPerServer, customData);

                    if (error != null)
                    {
                        // Get everything from the HttpContext
                        error.SetProperties(context);

                        if (error.LogToStore())
                        {
                            return error;
                        }
                    }
                }
                catch (Exception e)
                {
                    Exceptional.Settings.OnLogFailure?.Invoke(e);
                    Trace.WriteLine(e);
                }
            }
            return null;
        }

        /// <summary>
        /// Asynchronously logs an exception to the configured error store, or the in-memory default store if none is configured.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="context">The HTTPContext to record variables from.  If this isn't a web request, pass <see langword="null" /> in here.</param>
        /// <param name="category">The category to associate with this exception.</param>
        /// <param name="rollupPerServer">Whether to log up per-server, e.g. errors are only duplicates if they have same stack on the same machine.</param>
        /// <param name="customData">Any custom data to store with the exception like UserId, etc...this will be rendered as JSON in the error view for script use.</param>
        /// <param name="applicationName">If specified, the application name to log with, if not specified the name in <see cref="ErrorStoreSettings.ApplicationName"/> is used.</param>
        /// <returns>The Error created, if one was created and logged, null if nothing was logged.</returns>
        /// <remarks>
        /// When dealing with a non web requests, pass <see langword="null" /> in for context.  
        /// It shouldn't be forgotten for most web application usages, so it's not an optional parameter.
        /// </remarks>
        public static async Task<Error> LogAsync(
            this Exception ex,
            HttpContext context,
            string category = null,
            bool rollupPerServer = false,
            Dictionary<string, string> customData = null,
            string applicationName = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (Statics.IsLoggingEnabled)
            {
                try
                {
                    var settings = context.RequestServices.GetRequiredService<IOptions<ExceptionalSettings>>().Value;
                    // If we should be ignoring this exception, skip it entirely.
                    // Otherwise create the error itself, populating CustomData with what was passed-in.
                    var error = ex.GetErrorIfNotIgnored(settings, category, applicationName, rollupPerServer, customData);

                    if (error != null)
                    {
                        // Get everything from the HttpContext
                        error.SetProperties(context);

                        if (await error.LogToStoreAsync().ConfigureAwait(false))
                        {
                            return error;
                        }
                    }
                }
                catch (Exception e)
                {
                    Exceptional.Settings.OnLogFailure?.Invoke(e);
                    Trace.WriteLine(e);
                }
            }
            return null;
        }

        /// <summary>
        /// Sets Error properties pulled from HttpContext, if present.
        /// </summary>
        /// <param name="error">The error to set properties on.</param>
        /// <param name="context">The <see cref="HttpContext"/> related to the request.</param>
        /// <returns>The passed-in <see cref="Error"/> for chaining.</returns>
        private static void SetProperties(this Error error, HttpContext context)
        {
            if (error == null)
            {
                return;
            }

            var request = context.Request;

            NameValueCollection TryGetCollection(Func<HttpRequest, IEnumerable<KeyValuePair<string, StringValues>>> getter)
            {
                try
                {
                    var original = getter(request);
                    var copy = new NameValueCollection();
                    foreach (var kvp in original)
                    {
                        try
                        {
                            foreach (var value in kvp.Value)
                            {
                                copy.Add(kvp.Key, value);
                            }
                        }
                        catch (Exception e)
                        {
                            Trace.WriteLine(string.Format("Error getting collection value [{0}]: {1}", kvp.Key, e.Message));
                            copy.Add(kvp.Key, "[Error getting value: " + e.Message + "]");
                        }
                    }
                    return copy;
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Error parsing collection: " + e.Message);
                    return new NameValueCollection {[Constants.CollectionErrorKey] = e.Message };
                }
            }

            error.Host = request.Host.ToString();
            error.UrlPath = $"{request.PathBase}{request.Path}";
            error.FullUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
            error.HTTPMethod = request.Method;

            var exs = error.Settings as ExceptionalSettings;
            if (exs?.GetIPAddress != null)
            {
                try
                {
                    error.IPAddress = exs.GetIPAddress(context);
                }
                catch (Exception gipe)
                {
                    Trace.WriteLine("Error in GetIPAddress: " + gipe.Message);
                }
            }
            else
            {
                error.IPAddress = context.Connection?.RemoteIpAddress?.ToString();
            }

            error.ServerVariables = new NameValueCollection
            {
                ["ContentLength"] = request.ContentLength?.ToString(),
                ["ContentType"] = request.ContentType,
                ["Host"] = request.Host.Host,
                ["Path"] = request.Path,
                ["PathBase"] = request.PathBase,
                ["Port"] = request.Host.Port?.ToString(),
                ["Protocol"] = request.Protocol,
                ["QueryString"] = request.QueryString.Value,
                ["Request Method"] = request.Method,
                ["Scheme"] = request.Scheme,
                ["Url"] = error.FullUrl,
            };
            error.QueryString = TryGetCollection(r => r.Query);
            if (request.HasFormContentType)
            {
                error.Form = TryGetCollection(r => r.Form);
                // Filter form variables for sensitive information
                var formFilters = error.Settings?.LogFilters.Form;
                if (formFilters?.Count > 0)
                {
                    foreach (var kv in formFilters)
                    {
                        if (error.Form[kv.Key] != null)
                        {
                            error.Form[kv.Key] = kv.Value ?? string.Empty;
                        }
                    }
                }
            }

            error.Cookies = new NameValueCollection(request.Cookies.Count);
            foreach (var cookie in request.Cookies)
            {
                string val = null;
                error.Settings?.LogFilters.Cookie?.TryGetValue(cookie.Key, out val);
                error.Cookies.Add(cookie.Key, val ?? cookie.Value);
            }

            error.RequestHeaders = new NameValueCollection(request.Headers.Count);
            foreach (var header in request.Headers)
            {
                // Cookies are handled above, no need to repeat
                if (string.Compare(header.Key, "Cookie", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                string val = null;
                error.Settings?.LogFilters.Header?.TryGetValue(header.Key, out val);

                foreach (var v in header.Value)
                {
                    error.RequestHeaders.Add(header.Key, val ?? v);
                }
            }
        }
    }
}
