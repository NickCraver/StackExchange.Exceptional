using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Represents a class that outputs some HTML.
    /// </summary>
    public abstract class HtmlBase
    {
        /// <summary>
        /// Known ServerVariable keys to hide when rendering
        /// </summary>
        protected static readonly HashSet<string> HiddenHttpKeys = new HashSet<string>
        {
            "ALL_HTTP",
            "ALL_RAW",
            "HTTP_CONTENT_LENGTH",
            "HTTP_CONTENT_TYPE",
            "HTTP_COOKIE",
            "QUERY_STRING"
        };

        /// <summary>
        /// Known ServerVariable keys
        /// </summary>
        protected static readonly HashSet<string> DefaultHttpKeys = new HashSet<string>
        {
            "APPL_MD_PATH",
            "APPL_PHYSICAL_PATH",
            "GATEWAY_INTERFACE",
            "HTTP_ACCEPT",
            "HTTP_ACCEPT_CHARSET",
            "HTTP_ACCEPT_ENCODING",
            "HTTP_ACCEPT_LANGUAGE",
            "HTTP_CONNECTION",
            "HTTP_HOST",
            "HTTP_KEEP_ALIVE",
            "HTTPS",
            "INSTANCE_ID",
            "INSTANCE_META_PATH",
            "PATH_INFO",
            "PATH_TRANSLATED",
            "REMOTE_PORT",
            "SCRIPT_NAME",
            "SERVER_NAME",
            "SERVER_PORT",
            "SERVER_PORT_SECURE",
            "SERVER_PROTOCOL",
            "SERVER_SOFTWARE"
        };

        /// <summary>
        /// Renders the HTML for this item to the given <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The string builder to render to.</param>
        protected abstract void RenderHtml(StringBuilder sb);

        /// <summary>
        /// Renders to the HTML for this item.
        /// </summary>
        /// <returns>The HTML output.</returns>
        public string Render()
        {
            var sb = StringBuilderCache.Get();
            RenderHtml(sb);
            return sb.ToStringRecycle();
        }

        private static readonly Regex _sanitizeUrl = new Regex(@"[^-a-z0-9+&@#/%?=~_|!:,.;\*\(\)\{\}]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Sanitizes a URL for safety.
        /// </summary>
        /// <param name="url">The URL string to sanitize.</param>
        /// <returns>The sanitized URL.</returns>
        protected static string SanitizeUrl(string url) => url.IsNullOrEmpty() ? url : _sanitizeUrl.Replace(url, "");

        /// <summary>
        /// Linkifies a URL, returning an anchor-wrapped version if sane.
        /// </summary>
        /// <param name="s">The URL string to attempt to linkify.</param>
        /// <param name="color">The HTML color to use (hex code or name).</param>
        /// <returns>The linified string, or the encoded string if not a safe URL.</returns>
        protected string Linkify(string s, string color = "#3D85B0")
        {
            if (s.IsNullOrEmpty())
            {
                return string.Empty;
            }

            if (Regex.IsMatch(s, "%[A-Z0-9][A-Z0-9]"))
            {
                s = s.UrlDecode();
            }

            if (Regex.IsMatch(s, "^(https?|ftp|file)://"))
            {
                //@* || (Regex.IsMatch(s, "/[^ /,]+/") && !s.Contains("/LM"))*@ // block special case of "/LM/W3SVC/1"
                var sane = SanitizeUrl(s);
                if (sane == s) // only link if it's not suspicious
                    return $@"<a style=""color: {color};"" href=""{sane}"">{s.HtmlEncode()}</a>";
            }

            return s.HtmlEncode();
        }
    }
}
