using System;
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
    public static class Extensions
    {
        /// <summary>
        /// Returns true if <paramref name="type"/> is <paramref name="ancestorName"/>, or descendent from <paramref name="ancestorName"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <param name="ancestorName">The <see cref="Type"/> name to check for ancestry of.</param>
        public static bool IsDescendentOf(this Type type, string ancestorName)
        {
            if (type.FullName == ancestorName) return true;

            return type.BaseType != null && IsDescendentOf(type.BaseType, ancestorName);
        }

        /// <summary>
        /// Returns a humanized string indicating how long ago something happened, eg "3 days ago".
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
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes == 1 ? "1 min ago" : ts.Minutes.ToString() + " mins ago";
            }
            if (delta < 86400)  // 24 hrs * 60 mins * 60 sec
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
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes == 1 ? "in 1 minute" : "in " + ts.Minutes.ToString() + " minutes";
            }
            if (delta < 86400) // 24 hrs * 60 mins * 60 sec
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
        public static string TruncateWithEllipsis(this string s, int maxLength)
        {
            const string ellipsis = "...";
            return (s.HasValue() && s.Length > maxLength) ? (s.Truncate(maxLength - ellipsis.Length) + ellipsis) : s;
        }

        /// <summary>
        /// Appends s tring, HTML encoding the contents first.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
        /// <param name="s">The stirng to encode and append.</param>
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

        private static readonly Regex IPv4Regex = new Regex(@"\b([0-9]{1,3}\.){3}[0-9]{1,3}$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Retrieves the IP address of the current request -- handles proxies and private networks.
        /// </summary>
        /// <param name="serverVariables">The server variables collection to extract the IP from.</param>
        public static string GetRemoteIP(this NameValueCollection serverVariables)
        {
            var ip = serverVariables["REMOTE_ADDR"]; // could be a proxy -- beware
            var ipForwarded = serverVariables["HTTP_X_FORWARDED_FOR"];

            // check if we were forwarded from a proxy
            if (ipForwarded.HasValue())
            {
                ipForwarded = IPv4Regex.Match(ipForwarded).Value;
                if (ipForwarded.HasValue() && (IPNet.TryParse(ipForwarded, out var net) && !net.IsPrivate))
                    ip = ipForwarded;
            }

            return ip.HasValue() ? ip : UnknownIP;
        }

        /// <summary>
        /// Converts a string to a guid, or empty guid if empty or invalid.
        /// </summary>
        /// <param name="input">The string to attempt conversion to a guid.</param>
        public static Guid ToGuid(this string input)
        {
            Guid.TryParse(input, out Guid result);
            return result;
        }

        /// <summary>
        /// Strips non-essential characters (dashes!) from a guid.
        /// </summary>
        /// <param name="guid">The guid to string dashes from.</param>
        public static string ToFileName(this Guid guid) => guid.ToString().Replace("-", "");

        private static readonly char[] _dotSplit = new char[] { '.' };

        /// <summary>
        /// Gets the short exception name, e.g. "System.IndexOutOfRange" returns just "IndexOutOfRange".
        /// </summary>
        /// <param name="type">The type to get a short name for.</param>
        public static string ToShortException(this string type)
        {
            if (type.IsNullOrEmpty()) return string.Empty;
            var shortType = type.Split(_dotSplit).Last();

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
        /// Returns a unix Epoch time given a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dt">The <see cref="DateTime"/> to convert.</param>
        public static long ToEpochTime(this DateTime dt) => (long)(dt - Epoch).TotalSeconds;

        /// <summary>
        /// Returns a unix Epoch time if given a value, and null otherwise.
        /// </summary>
        /// <param name="dt">The <see cref="DateTime"/> to convert.</param>
        public static long? ToEpochTime(this DateTime? dt) => dt.HasValue ? (long?)ToEpochTime(dt.Value) : null;

        /// <summary>
        /// Takes a NameValuePair collection and reduces it down to a JSON object in kay/value pair form.
        /// </summary>
        /// <param name="collection">The collection to convert to a dictionary.</param>
        /// <remarks>
        /// This is not technically correct for all cases, since a querystring can contain multiple 
        /// occurences of the same variable for example, this reduces it down to 1 occurence for the acessibility tradeoff.
        /// </remarks>
        public static Dictionary<string, string> ToJsonDictionary(this List<Error.NameValuePair> collection)
        {
            var result = new Dictionary<string, string>();
            if (collection == null) return result;
            foreach (var pair in collection)
            {
                if (pair.Name.HasValue())
                {
                    result[pair.Name] = pair.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// Turns a dictionary into a name value collection, for code reuse.
        /// </summary>
        /// <param name="dict">The dictionary to convert.</param>
        public static NameValueCollection ToNameValueCollection(this Dictionary<string, string> dict)
        {
            if (dict == null) return new NameValueCollection();

            var result = new NameValueCollection(dict.Count);
            foreach (var kvp in dict)
            {
                result.Add(kvp.Key, kvp.Value);
            }
            return result;
        }
    }
}
