using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Extensions methods for <see cref="Error"/>.
    /// </summary>
    public static class ErrorExtensions
    {
        /// <summary>
        /// For logging an exception with no HttpContext, most commonly used in non-web applications 
        /// so that they don't have to carry a reference to System.Web
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="appendFullStackTrace">Whether to append a full stack trace to the exception's detail</param>
        /// <param name="rollupPerServer">Whether to log up per-server, e.g. errors are only duplicates if they have same stack on the same machine</param>
        /// <param name="customData">Any custom data to store with the exception like UserId, etc...this will be rendered as JSON in the error view for script use</param>
        public static Error LogWithoutContext(this Exception ex, bool appendFullStackTrace = false, bool rollupPerServer = false, Dictionary<string, string> customData = null)
        {
            return Log(ex, null, appendFullStackTrace, rollupPerServer, customData);
        }

        /// <summary>
        /// Logs an exception to the configured error store, or the in-memory default store if none is configured.
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="context">The HTTPContext to record variables from.  If this isn't a web request, pass <see langword="null" /> in here.</param>
        /// <param name="appendFullStackTrace">Whether to append a full stack trace to the exception's detail.</param>
        /// <param name="rollupPerServer">Whether to log up per-server, e.g. errors are only duplicates if they have same stack on the same machine.</param>
        /// <param name="customData">Any custom data to store with the exception like UserId, etc...this will be rendered as JSON in the error view for script use.</param>
        /// <param name="applicationName">If specified, the application name to log with, if not specified the name in the config is used.</param>
        /// <returns>The Error created, if one was created and logged, null if nothing was logged.</returns>
        /// <remarks>
        /// When dealing with a non web requests, pass <see langword="null" /> in for context.  
        /// It shouldn't be forgotten for most web application usages, so it's not an optional parameter.
        /// </remarks>
        public static Error Log(this Exception ex, HttpContext context, bool? appendFullStackTrace = null, bool rollupPerServer = false, Dictionary<string, string> customData = null, string applicationName = null)
        {
            if (!ExceptionalSettings.IsLoggingEnabled) return null;
            try
            {
                Settings.LoadSettings();
                var settings = ExceptionalSettings.Current;
                if (settings.Ignore.Regexes?.Any(re => re.IsMatch(ex.ToString())) == true)
                    return null;
                if (settings.Ignore.Types?.Any(type => ex.GetType().IsDescendentOf(type)) == true)
                    return null;

                if (customData == null && TODOShittyExperienceForTheUser.GetCustomData != null)
                {
                    customData = new Dictionary<string, string>();
                    try
                    {
                        TODOShittyExperienceForTheUser.GetCustomData(ex, context, customData);
                    }
                    catch (Exception cde)
                    {
                        // if there was an error getting custom errors, log it so we can display such in the view...and not fail to log the original error
                        customData.Add(Constants.CustomDataErrorKey, cde.ToString());
                    }
                }

                var error = new Error(ex, applicationName)
                {
                    RollupPerServer = rollupPerServer,
                    CustomData = customData ?? new Dictionary<string, string>()
                };

                if (ex is HttpException httpException)
                {
                    error.StatusCode = httpException.GetHttpCode();
                }

                // Get everything from the HttpContext
                error.SetContextProperties(context);

                if (settings.GetIPAddress != null)
                {
                    try
                    {
                        error.IPAddress = settings.GetIPAddress();
                    }
                    catch (Exception gipe)
                    {
                        // if there was an error getting the IP, log it so we can display such in the view...and not fail to log the original error
                        error.CustomData.Add(Constants.CustomDataErrorKey, "Fetching IP Adddress: " + gipe);
                    }
                }

                if (appendFullStackTrace ?? settings.AppendFullStackTraces)
                {
                    var frames = new StackTrace(fNeedFileInfo: true).GetFrames();
                    if (frames?.Length > 2)
                        error.Detail += "\n\nFull Trace:\n\n" + string.Join("", frames.Skip(2));
                    error.ErrorHash = error.GetHash();
                }

                var logged = error.LogToStore(ErrorStore.Default);
                return logged ? error : null;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                return null;
            }
        }

        /// <summary>
        /// Sets Error properties pulled from HttpContext, if present.
        /// </summary>
        /// <param name="error">The error to set properties on.</param>
        /// <param name="context">The <see cref="HttpContext"/> related to the request.</param>
        public static void SetContextProperties(this Error error, HttpContext context)
        {
            if (context == null || context.Handler == null) return;

            var request = context.Request;

            Func<Func<HttpRequest, NameValueCollection>, NameValueCollection> tryGetCollection = getter =>
            {
                try
                {
                    var original = getter(request);
                    var copy = new NameValueCollection();
                    foreach (var key in original.AllKeys)
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
                    return copy;
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Error parsing collection: " + e.Message);
                    return new NameValueCollection {[Constants.CollectionErrorKey] = e.Message };
                }
            };

            error.ServerVariables = tryGetCollection(r => r.ServerVariables);
            error.QueryString = tryGetCollection(r => r.QueryString);
            error.Form = tryGetCollection(r => r.Form);

            // Filter form variables for sensitive information
            var formFilters = ExceptionalSettings.Current.LogFilters.Form;
            if (formFilters?.Count > 0)
            {
                foreach (var k in formFilters.Keys)
                {
                    if (error.Form[k] != null)
                        error.Form[k] = formFilters[k];
                }
            }

            try
            {
                error.Cookies = new NameValueCollection(request.Cookies.Count);
                for (var i = 0; i < request.Cookies.Count; i++)
                {
                    var name = request.Cookies[i].Name;
                    string val = null;
                    ExceptionalSettings.Current.LogFilters.Cookie?.TryGetValue(name, out val);
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

                if (request.Headers[header] != null)
                    error.RequestHeaders[header] = request.Headers[header];
            }
        }
    }
}
