using System.Collections.Specialized;
using System.Linq;
using System.Text;
using StackExchange.Exceptional.Internal;
using System;

namespace StackExchange.Exceptional.Pages
{
    /// <summary>
    /// An error detail page for showing the specifics on a single error.
    /// </summary>
    public class ErrorDetailPage : WebPage
    {
        private readonly Guid _guid;

        /// <summary>
        /// Creates an <see cref="ErrorDetailPage"/>.
        /// </summary>
        /// <param name="error">The error we're rendering details.</param>
        /// <param name="store">The store this error is from.</param>
        /// <param name="baseURL">The base URL for the current request.</param>
        /// <param name="guid">The id for the error (populate even if the error is null)</param>
        public ErrorDetailPage(Error error, ErrorStore store, string baseURL, Guid guid)
            : base(error, store, baseURL, "Error - " + (error?.Message ?? "Not Found"))
        {
            _guid = guid;
        }

        /// <summary>
        /// Renders the main contents of the page.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to render to.</param>
        protected override void RenderInnerHtml(StringBuilder sb)
        {
            void RenderVariableTable(string title, string className, NameValueCollection vars)
            {
                if (vars == null || vars.Count == 0) return;

                var fetchError = vars[Constants.CollectionErrorKey];
                var errored = fetchError.HasValue();
                var keys = vars.AllKeys.Where(key => !HiddenHttpKeys.Contains(key) && key != Constants.CollectionErrorKey).OrderBy(k => k);

                sb.AppendFormat("  <div class=\"{0}\">", className).AppendLine();
                if (errored)
                {
                    sb.AppendFormat("    <h3 class=\"title-error\">{0} - Error while gathering data</h3>", title).AppendLine();
                }
                else
                {
                    sb.AppendFormat("    <h3>{0}</h3>", title).AppendLine();
                }
                if (keys.Any())
                {
                    var hiddenRows = new StringBuilder();
                    sb.AppendLine("    <div class=\"side-scroll\">")
                      .AppendLine("      <table class=\"alt-rows key-value\">")
                      .AppendLine("        <tbody>");
                    foreach (var k in keys)
                    {
                        // If this has no value, skip it
                        if (vars[k].HasValue())
                        {
                            // If this is a hidden row, buffer it up, since CSS has no clean mechanism for :visible:nth-row(odd) type styling behavior
                            (DefaultHttpKeys.Contains(k) ? hiddenRows : sb).AppendFormat("        <tr><td>{0}</td><td>{1}</td></tr>", k, Linkify(vars[k])).AppendLine();
                        }
                    }
                    if (vars["HTTP_HOST"].HasValue() && vars["URL"].HasValue())
                    {
                        var ssl = vars["HTTP_X_FORWARDED_PROTO"] == "https" || vars["HTTP_X_SSL"].HasValue() || vars["HTTPS"] == "on";
                        var url = string.Format("http{3}://{0}{1}{2}", vars["HTTP_HOST"], vars["URL"], vars["QUERY_STRING"].HasValue() ? "?" + vars["QUERY_STRING"] : "", ssl ? "s" : "");
                        sb.AppendFormat("        <tr><td>URL and Query</td><td>{0}</td></tr>", vars["REQUEST_METHOD"] == "GET" ? Linkify(url) : url.HtmlEncode()).AppendLine();
                    }
                    sb.AppendLine("        </tbody>");
                    if (hiddenRows.Length > 0)
                    {
                        sb.AppendLine("        <tbody class=\"hidden\">")
                          .Append(hiddenRows)
                          .AppendLine("        </tbody>");
                    }
                    sb.AppendLine("      </table>")
                      .AppendLine("    </div>");
                }
                if (errored)
                {
                    sb.AppendFormat("<span class=\"custom-error-label\">Get {0} threw an exception:</span>", title)
                      .AppendFormat("<pre class=\"stack\"><code>{0}</code></pre>", fetchError.HtmlEncode());
                }
                sb.AppendFormat("  </div>");
            }

            if (Error == null)
            {
                sb.AppendFormat("  <h1 class=\"not-found\">Oh no! Error {0} was not found!</h1>", _guid.ToString()).AppendLine();
            }
            else
            {
                sb.Append("  <h1>").AppendHtmlEncode(Error.Message).AppendLine("</h1>")
                  .Append("  <div class=\"subtitle\">").AppendHtmlEncode(Error.Type);
                if (Error.DuplicateCount > 1)
                {
                    sb.Append(" <span class=\"duplicate-count\">(thrown ").Append(Error.DuplicateCount.Value).AppendLine(" times)</span>");
                }
                sb.AppendLine("</div>")
                  .Append("  <pre class=\"stack dark\"><code>").Append(Utils.StackTrace.HtmlPrettify(Error.Detail)).AppendLine().AppendLine("</code></pre>")
                  // TODO: Controls for show/hide of async .stack.row.async in the block above
                  // TODO: Remove - temporarily showing the raw while the user-friendlier display above gets tuned
                  .Append("  <pre class=\"stack\"><code>").AppendHtmlEncode(Error.Detail).AppendLine().AppendLine("</code></pre>")
                  .Append("  <p class=\"sub-info\">occurred <b title=\"")
                  .AppendHtmlEncode(Error.CreationDate.ToLongDateString()).Append(" at ").AppendHtmlEncode(Error.CreationDate.ToLongTimeString())
                  .Append("\">")
                  .Append(Error.CreationDate.ToRelativeTime())
                  .Append("</b> on ")
                  .Append(" <span>(<a href=\"delete?guid=").Append(Error.GUID.ToString()).AppendLine("\">delete</a>)</span></p>");
                if (Error.SQL.HasValue())
                {
                    sb.AppendLine("  <h3>SQL</h3>")
                      .Append("  <pre class=\"command prettyprint lang-sql\"><code>")
                      .AppendHtmlEncode(Error.SQL)
                      .AppendLine("</code></pre>");
                }
                RenderVariableTable("Server Variables", "server-variables", Error.ServerVariables);

                if (Error.CustomData?.Count > 0)
                {
                    var errored = Error.CustomData.ContainsKey(Constants.CustomDataErrorKey);
                    var cdKeys = Error.CustomData.Keys.Where(k => k != Constants.CustomDataErrorKey);
                    sb.AppendLine("  <div class=\"custom-data\">");
                    if (errored)
                    {
                        sb.AppendLine("    <h3 class=\"title-error\">Custom - Error while gathering custom data</h3>");
                    }
                    else
                    {
                        sb.AppendLine("    <h3>Custom</h3>");
                    }
                    if (cdKeys.Any(k => k != Constants.CustomDataErrorKey))
                    {
                        sb.AppendLine("    <div class=\"side-scroll\">")
                          .AppendLine("      <table class=\"alt-rows key-value\">");
                        foreach (var cd in cdKeys)
                        {
                            sb.Append("        <tr>")
                              .Append("<td>").AppendHtmlEncode(cd).Append("</td>")
                              .Append("<td>").Append(Linkify(Error.CustomData[cd])).Append("</td>")
                              .AppendLine("</tr>");
                        }
                        sb.AppendLine("      </table>")
                          .AppendLine("    </div>");
                    }
                    if (errored)
                    {
                        sb.AppendLine("    <span class=\"custom-error-label\">GetCustomData threw an exception:</span>")
                          .AppendLine("    <pre class=\"stack\"><code>").Append(Error.CustomData[Constants.CustomDataErrorKey]).AppendLine("</code></pre>");
                    }
                    sb.AppendLine("  </div>");
                }
                RenderVariableTable("QueryString", "querystring", Error.QueryString);
                RenderVariableTable("Form", "form", Error.Form);
                RenderVariableTable("Cookies", "cookies", Error.Cookies);
                RenderVariableTable("RequestHeaders", "headers", Error.RequestHeaders);
            }
        }
    }
}