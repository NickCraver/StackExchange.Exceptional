using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Extensions methods for logging an <see cref="Exception"/>.
    /// </summary>
    public static class AspNetExtensions
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
            try
            {
                // If we should be ignoring this exception, skip it entirely.
                // Otherwise create the error itself, populating CustomData with what was passed-in.
                var error = ex.GetErrorIfNotIgnored(Exceptional.Settings, category, applicationName, rollupPerServer, customData);

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
                Exceptional.Settings?.OnLogFailure?.Invoke(e);
                Trace.WriteLine(e);
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
        public static Task<Error> LogAsync(
            this Exception ex,
            HttpContext context,
            string category = null,
            bool rollupPerServer = false,
            Dictionary<string, string> customData = null,
            string applicationName = null)
        {
            try
            {
                // If we should be ignoring this exception, skip it entirely.
                // Otherwise create the error itself, populating CustomData with what was passed-in.
                var error = ex.GetErrorIfNotIgnored(Exceptional.Settings, category, applicationName, rollupPerServer, customData);
                if (error != null)
                {
                    // Get everything from the HttpContext
                    error.SetProperties(context);

                    return LogAsyncCore(error);
                }
            }
            catch (Exception e)
            {
                Exceptional.Settings?.OnLogFailure?.Invoke(e);
                Trace.WriteLine(e);
            }
            return Task.FromResult<Error>(null);
        }

        private static async Task<Error> LogAsyncCore(Error error)
        {
            if (await error.LogToStoreAsync().ConfigureAwait(false))
            {
                return error;
            }
            return null;
        }

        private static bool ShouldRecordServerVariable(string name)
        {
            // All HTTP_ are duplicates of headers
            if (name?.StartsWith("HTTP_") == true)
            {
                return false;
            }
            switch (name)
            {
                case "CONTENT_LENGTH":
                case "REQUEST_METHOD":
                case "URL":
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Sets Error properties pulled from HttpContext, if present.
        /// </summary>
        /// <param name="error">The error to set properties on.</param>
        /// <param name="context">The <see cref="HttpContext"/> related to the request.</param>
        /// <returns>The passed-in <see cref="Error"/> for chaining.</returns>
        private static Error SetProperties(this Error error, HttpContext context)
        {
            if (error == null)
            {
                return null;
            }

            if (error.Exception is HttpException httpException)
            {
                error.StatusCode = httpException.GetHttpCode();
            }
            if (context == null || context.Handler == null)
            {
                return error;
            }

            var request = context.Request;

            NameValueCollection TryGetCollection(Func<HttpRequest, NameValueCollection> getter, Func<string, bool> shouldRecord = null)
            {
                try
                {
                    var original = getter(request);
                    var copy = new NameValueCollection();
                    foreach (var key in original.AllKeys)
                    {
                        if (shouldRecord?.Invoke(key) ?? true)
                        {
                            try
                            {
                                foreach (var value in original.GetValues(key))
                                {
                                    copy.Add(key, value);
                                }
                            }
                            catch (Exception e)
                            {
                                Trace.WriteLine(string.Format("Error getting collection value [{0}]: {1}", key, e.Message));
                                copy.Add(key, "[Error getting value: " + e.Message + "]");
                            }
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

            error.Host = request.Url?.Host;
            error.UrlPath = request.ServerVariables?["URL"]; // Legacy compatibility, this may be the same as request.Url.AbsolutePath;
            error.FullUrl = request.Url.ToString();
            error.HTTPMethod = request.HttpMethod;

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
                error.IPAddress = request.ServerVariables?.GetRemoteIP();
            }

            error.ServerVariables = TryGetCollection(r => r.ServerVariables, ShouldRecordServerVariable);

            error.QueryString = TryGetCollection(r => r.QueryString);
            // Filter query variables for sensitive information
            var queryFilters = error.Settings.LogFilters.QueryString;
            if (queryFilters?.Count > 0 && error.QueryString.Count > 0)
            {
                var oldQuery = request.Url.Query;
                var newQuery = oldQuery;
                foreach (var kv in queryFilters)
                {
                    if (error.QueryString[kv.Key] != null)
                    {
                        newQuery = newQuery.QueryStringReplace(kv.Key, kv.Value);
                        error.QueryString[kv.Key] = kv.Value ?? "";
                    }
                }
                error.FullUrl = error.FullUrl.Replace(oldQuery, newQuery);
            }

            error.Form = TryGetCollection(r => r.Form);
            // Filter form variables for sensitive information
            var formFilters = error.Settings.LogFilters.Form;
            if (formFilters?.Count > 0)
            {
                foreach (var kv in formFilters)
                {
                    if (error.Form[kv.Key] != null)
                    {
                        error.Form[kv.Key] = kv.Value ?? "";
                    }
                }
            }

            try
            {
                error.Cookies = new NameValueCollection(request.Cookies.Count);
                for (var i = 0; i < request.Cookies.Count; i++)
                {
                    var name = request.Cookies[i].Name;
                    string val = null;
                    error.Settings.LogFilters.Cookie?.TryGetValue(name, out val);
                    error.Cookies.Add(name, val ?? request.Cookies[i].Value);
                }
            }
            catch (HttpRequestValidationException e)
            {
                Trace.WriteLine("Error parsing cookie collection: " + e.Message);
            }

            error.RequestHeaders = new NameValueCollection(request.Headers.Count);
            foreach (var header in request.Headers.AllKeys)
            {
                // Cookies are handled above, no need to repeat
                if (string.Compare(header, "Cookie", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                string val = null;
                error.Settings?.LogFilters.Header?.TryGetValue(header, out val);

                if (request.Headers[header] != null)
                    error.RequestHeaders[header] = val ?? request.Headers[header];
            }

            return error;
        }
    }
}
