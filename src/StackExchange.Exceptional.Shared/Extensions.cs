using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Extensions methods for <see cref="Exception"/>s.
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// For logging an exception with no HttpContext, most commonly used in non-web applications 
        /// so that they don't have to carry a reference to System.Web.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="category">The category to associate with this exception.</param>
        /// <param name="rollupPerServer">Whether to log up per-server, e.g. errors are only duplicates if they have same stack on the same machine.</param>
        /// <param name="customData">Any custom data to store with the exception like UserId, etc...this will be rendered as JSON in the error view for script use.</param>
        /// <param name="applicationName">If specified, the application name to log with, if not specified the name in <see cref="ErrorStoreSettings.ApplicationName"/> is used.</param>
        public static Error LogNoContext(
            this Exception ex,
            string category = null,
            bool rollupPerServer = false,
            Dictionary<string, string> customData = null,
            string applicationName = null)
        {
            try
            {
                // If we should be ignoring this exception, skip it entirely.
                // Otherwise create the error itself, populating CustomData with what was passed-in.
                var error = ex.GetErrorIfNotIgnored(Statics.Settings, category, applicationName, rollupPerServer, customData);

                if (error?.LogToStore() == true)
                {
                    return error;
                }
            }
            catch (Exception e)
            {
                Statics.Settings?.OnLogFailure?.Invoke(e);
                Trace.WriteLine(e);
            }
            return null;
        }

        /// <summary>
        /// Adds a key/value pair for logging to an exception, one that'll appear in exceptional
        /// </summary>
        /// <typeparam name="T">The specific type of exception (for return type chaining).</typeparam>
        /// <param name="ex">The exception itself.</param>
        /// <param name="key">The key to add to the exception data.</param>
        /// <param name="value">The value to add to the exception data.</param>
        public static T AddLogData<T>(this T ex, string key, string value) where T : Exception
        {
            ex.Data[Constants.CustomDataKeyPrefix + key] = value ?? string.Empty;
            return ex;
        }

        /// <summary>
        /// Adds a key/value pair for logging to an exception, one that'll appear in exceptional
        /// </summary>
        /// <typeparam name="T">The specific type of exception (for return type chaining).</typeparam>
        /// <param name="ex">The exception itself.</param>
        /// <param name="key">The key to add to the exception data.</param>
        /// <param name="value">The value to add to the exception data.</param>
        public static T AddLogData<T>(this T ex, string key, object value) where T : Exception => AddLogData(ex, key, value?.ToString());
    }
}
