using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Internal Exceptional extensions, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// </summary>
    public static class InternalExtensions
    {
        /// <summary>
        /// Returns if an exception should be ignored according to the passed-in <see cref="ExceptionalSettingsBase"/>.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <param name="settings">The settings to check <paramref name="ex"/> against.</param>
        /// <returns>Whether this exception should be ignored.</returns>
        public static bool ShouldBeIgnored(this Exception ex, ExceptionalSettingsBase settings)
        {
            if (ex == null)
            {
                return true;
            }
            var exString = ex.ToString();
            return settings.Ignore.Regexes?.Any(re => re.IsMatch(exString)) == true
                || settings.Ignore.Types?.Any(type => ex.GetType().IsDescendentOf(type)) == true;
        }

        /// <summary>
        /// Gets an <see cref="Error"/> if current settings don't tell us to ignore it.
        /// </summary>
        /// <param name="ex">The exception to consider.</param>
        /// <param name="category">The category of the exception.</param>
        /// <param name="applicationName">The application name to log under.</param>
        /// <param name="rollupPerServer">Whether to rollup by server.</param>
        /// <param name="customData">The custom data, if any, to pass in.</param>
        /// <param name="settings">The specific settings to use.</param>
        /// <returns>An error if not ignored, <c>null</c> if ignored.</returns>
        public static Error GetErrorIfNotIgnored(
            this Exception ex,
            ExceptionalSettingsBase settings,
            string category = null,
            string applicationName = null,
            bool rollupPerServer = false,
            Dictionary<string, string> customData = null)
        {
            if (!Statics.IsLoggingEnabled)
            {
                return null;
            }

            // If we should be ignoring this exception, skip it entirely.
            if (!ex.ShouldBeIgnored(settings))
            {
                // Create the error itself, populating CustomData with what was passed-in.
                return new Error(ex, settings, category, applicationName, rollupPerServer, customData);
            }
            return null;
        }

        /// <summary>
        /// Returns if the type of the exception is built into the .NET framework.
        /// </summary>
        /// <param name="e">The exception to check.</param>
        /// <returns>True if the exception is a type from within the CLR, false if it's a user/third party type.</returns>
        public static bool IsBCLException(this Exception e) => e.GetType().Module.ScopeName == "CommonLanguageRuntimeLibrary";

        /// <summary>
        /// Returns <c>true</c> if <paramref name="type"/> is <paramref name="ancestorName"/>, or descendant from <paramref name="ancestorName"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <param name="ancestorName">The <see cref="Type"/> name to check for ancestry of.</param>
        public static bool IsDescendentOf(this Type type, string ancestorName)
        {
            if (type.FullName == ancestorName) return true;

            return type.BaseType != null && IsDescendentOf(type.BaseType, ancestorName);
        }

        /// <summary>
        /// Returns a humanized string indicating how long ago something happened, e.g. "3 days ago".
        /// For future dates, returns when this DateTime will occur from <see cref="DateTime.UtcNow"/>.
        /// </summary>
        /// <param name="dt">The <see cref="DateTime"/> to represents as a relative time string.</param>
        public static string ToRelativeTime(this DateTime dt)
        {
            DateTime utcNow = DateTime.UtcNow;
            return dt <= utcNow ? ToRelativeTimePast(dt, utcNow) : ToRelativeTimeFuture(dt, utcNow);
        }

        private static string ToRelativeTimePast(DateTime dt, DateTime utcNow)
        {
            TimeSpan ts = utcNow - dt;
            double delta = ts.TotalSeconds;

            if (delta < 60)
            {
                return ts.Seconds == 1 ? "1 sec ago" : ts.Seconds.ToString() + " secs ago";
            }
            if (delta < 3600) // 60 minutes * 60 seconds
            {
                return ts.Minutes == 1 ? "1 min ago" : ts.Minutes.ToString() + " mins ago";
            }
            if (delta < 86400)  // 24 hours * 60 minutes * 60 seconds
            {
                return ts.Hours == 1 ? "1 hour ago" : ts.Hours.ToString() + " hours ago";
            }

            int days = ts.Days;
            if (days == 1)
            {
                return "yesterday";
            }
            if (days <= 2)
            {
                return days.ToString() + " days ago";
            }
            if (days <= 330)
            {
                return dt.ToString("MMM %d 'at' %H:mmm");
            }
            return dt.ToString(@"MMM %d \'yy 'at' %H:mmm");
        }

        private static string ToRelativeTimeFuture(DateTime dt, DateTime utcNow)
        {
            TimeSpan ts = dt - utcNow;
            double delta = ts.TotalSeconds;

            if (delta < 60)
            {
                return ts.Seconds == 1 ? "in 1 second" : "in " + ts.Seconds.ToString() + " seconds";
            }
            if (delta < 3600) // 60 minutes * 60 seconds
            {
                return ts.Minutes == 1 ? "in 1 minute" : "in " + ts.Minutes.ToString() + " minutes";
            }
            if (delta < 86400) // 24 hours * 60 minutes * 60 seconds
            {
                return ts.Hours == 1 ? "in 1 hour" : "in " + ts.Hours.ToString() + " hours";
            }

            // use our own rounding so we can round the correct direction for future
            var days = (int)Math.Round(ts.TotalDays, 0);
            if (days == 1)
            {
                return "tomorrow";
            }
            if (days <= 10)
            {
                return "in " + days.ToString() + " day" + (days > 1 ? "s" : "");
            }
            if (days <= 330)
            {
                return "on " + dt.ToString("MMM %d 'at' %H:mmm");
            }
            return "on " + dt.ToString(@"MMM %d \'yy 'at' %H:mmm");
        }

        /// <summary>
        /// Force string to be <paramref name="maxLength"/> or smaller.
        /// </summary>
        /// <param name="s">The <see cref="string"/> to truncate.</param>
        /// <param name="maxLength">The length to truncate the string to.</param>
        public static string Truncate(this string s, int maxLength) =>
            (s.HasValue() && s.Length > maxLength) ? s.Remove(maxLength) : s;

        /// <summary>
        /// If this <see cref="string"/> is over <paramref name="maxLength"/>, answers a new <see cref="string"/> 
        /// with Length = <paramref name="maxLength"/> and ... as the final three characters.
        /// </summary>
        /// <param name="s">The <see cref="string"/> to truncate.</param>
        /// <param name="maxLength">The length to truncate the string to.</param>
        public static string EncodeTruncateWithEllipsis(this string s, int maxLength) =>
            (s.HasValue() && s.Length > maxLength) ? (s.Truncate(maxLength - 1).HtmlEncode() + "&hellip;") : s.HtmlEncode();

        /// <summary>
        /// Appends a <see cref="string"/>, HTML encoding the contents first.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
        /// <param name="s">The <see cref="string"/> to encode and append.</param>
        /// <returns>The original <see cref="StringBuilder"/> for chaining.</returns>
        public static StringBuilder AppendHtmlEncode(this StringBuilder sb, string s) => sb.Append(s.HtmlEncode());

        /// <summary>
        /// Encodes the string as HTML.
        /// </summary>
        /// <param name="s">The dangerous string to encode.</param>
        /// <returns>The safely encoded HTML string.</returns>
        public static string HtmlEncode(this string s) => s.HasValue() ? WebUtility.HtmlEncode(s) : s;

        /// <summary>
        /// Decodes an HTML string.
        /// </summary>
        /// <param name="s">The HTML-encoded string to decode.</param>
        /// <returns>The decoded HTML string.</returns>
        public static string HtmlDecode(this string s) => s.HasValue() ? WebUtility.HtmlDecode(s) : s;

        /// <summary>
        /// Encodes the string for URLs.
        /// </summary>
        /// <param name="s">The dangerous string to URL encode.</param>
        /// <returns>The safely encoded URL string.</returns>
        public static string UrlEncode(this string s) => s.HasValue() ? WebUtility.UrlEncode(s) : s;

        /// <summary>
        /// Decodes a URL-encoded string.
        /// </summary>
        /// <param name="s">The URL-encoded string to decode.</param>
        /// <returns>The decoded string.</returns>
        public static string UrlDecode(this string s) => s.HasValue() ? WebUtility.UrlDecode(s) : s;

        /// <summary>
        /// Answers true if this String is either null or empty.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <remarks>I'm so tired of typing String.IsNullOrEmpty(s)</remarks>
        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <remarks>I'm also tired of typing !String.IsNullOrEmpty(s)</remarks>
        public static bool HasValue(this string s) => !IsNullOrEmpty(s);

        /// <summary>
        /// When a client IP can't be determined
        /// </summary>
        public const string UnknownIP = "0.0.0.0";

        private static readonly char[] _commaSpace = new char[] { ',', ' ' };

        /// <summary>
        /// Retrieves the IP address of the current request -- handles proxies and private networks.
        /// </summary>
        /// <param name="serverVariables">The server variables collection to extract the IP from.</param>
        public static string GetRemoteIP(this NameValueCollection serverVariables)
        {
            var ip = serverVariables["REMOTE_ADDR"]; // could be a proxy -- beware
            var forwardedFor = serverVariables["HTTP_X_FORWARDED_FOR"] ?? "";

            var remoteIPs = forwardedFor.Split(_commaSpace, StringSplitOptions.RemoveEmptyEntries);
            // Loop from the end until we get the first IP that's *not* internal:
            for (var i = remoteIPs.Length - 1; i >= 0; i--)
            {
                var remoteIp = remoteIPs[i];
                // Nothing? Toss it.
                if (remoteIp == null) continue;
                // Not valid? Toss it.
                if (!IPNet.TryParse(remoteIp, out IPNet remoteIpNet)) continue;
                // Usual prod behavior: Never match a private address, unless it's the last.
                // Get the first IP outside current networks.
                // Take the first external IP we match, or the last IP we match - e.g. from another web server
                if (!remoteIpNet.IsPrivate || i == 0)
                {
                    ip = remoteIp;
                    break;
                }
            }

            return ip.HasValue() ? ip : UnknownIP;
        }

        /// <summary>
        /// Converts a string to a GUID, or <see cref="Guid.Empty"/> if empty or invalid.
        /// </summary>
        /// <param name="input">The <see cref="string"/> to attempt conversion to a <see cref="Guid"/>.</param>
        public static Guid ToGuid(this string input)
        {
            Guid.TryParse(input, out Guid result);
            return result;
        }

        /// <summary>
        /// Gets the short exception name, e.g. "System.IndexOutOfRange" returns just "IndexOutOfRange".
        /// </summary>
        /// <param name="type">The type to get a short name for.</param>
        public static string ToShortTypeName(this string type)
        {
            if (type.IsNullOrEmpty()) return string.Empty;
            var lastIndex = type.LastIndexOf('.');
            var shortType = lastIndex > -1 && lastIndex + 1 < type.Length ? type.Substring(lastIndex + 1) : type;

            const string suffix = "Exception";

            if (shortType.EndsWith(suffix) && shortType != suffix)
                return shortType.Substring(0, shortType.Length - suffix.Length);
            return shortType;
        }

        /// <summary>
        /// Resolves ~\ relative paths if needed.
        /// </summary>
        /// <param name="path">The path to resolve.</param>
        public static string ResolvePath(this string path)
        {
            // TODO: Move this to StackExchange.Exceptional settings conversion.
            if (path.IsNullOrEmpty()) return "";
            if (path.StartsWith(@"~\"))
                return AppDomain.CurrentDomain.GetData("APPBASE") + path.Substring(2);
            return path;
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        /// <summary>
        /// Returns a Unix Epoch time given a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dt">The <see cref="DateTime"/> to convert.</param>
        public static long ToEpochTime(this DateTime dt) => (long)(dt - Epoch).TotalSeconds;

        /// <summary>
        /// Returns a Unix Epoch time if given a value, and null otherwise.
        /// </summary>
        /// <param name="dt">The <see cref="DateTime"/> to convert.</param>
        public static long? ToEpochTime(this DateTime? dt) => dt.HasValue ? (long?)ToEpochTime(dt.Value) : null;

        private static readonly ConcurrentDictionary<string, Regex> _regexCache = new ConcurrentDictionary<string, Regex>();

        /// <summary>
        /// Replaces a QueryString token with the replacement value.
        /// </summary>
        /// <param name="queryString">The querystring to operate on.</param>
        /// <param name="key">The key to look for.</param>
        /// <param name="value">The value to replace.</param>
        /// <returns>The updated query string.</returns>
        public static string QueryStringReplace(this string queryString, string key, string value)
        {
            var regex = _regexCache.GetOrAdd(key, k => new Regex("(?<=[?&]" + Regex.Escape(k) + "=)[^&]*", RegexOptions.IgnoreCase | RegexOptions.Compiled));
            return regex.Replace(queryString, value);
        }
    }
}
