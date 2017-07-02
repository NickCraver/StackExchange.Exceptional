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

                sb.AppendFormat("  <div class=\"{0}\">", className)
                  .AppendFormat("    <h3 class=\"kv-title{1}\">{0}{2}</h3>", title, errored ? " title-error" : "", errored ? " - Error while gathering data" : "");
                if (keys.Any())
                {
                    var hiddenRows = new StringBuilder();
                    sb.AppendLine("    <div class=\"side-scroll\">")
                      .AppendLine("      <table class=\"kv-table\">");
                    foreach (var k in keys)
                    {
                        // If this has no value, skip it
                        if (vars[k].IsNullOrEmpty())
                        {
                            continue;
                        }
                        // If this is a hidden row, buffer it up, since CSS has no clean mechanism for :visible:nth-row(odd) type styling behavior
                        var hidden = DefaultHttpKeys.Contains(k);
                        var toWrite = hidden ? hiddenRows : sb;
                        toWrite.AppendFormat("        <tr{2}><td class=\"key\">{0}</td><td class=\"value\">{1}</td></tr>", k, Linkify(vars[k]), hidden ? " class=\"hidden\"" : "");
                    }
                    if (vars["HTTP_HOST"].HasValue() && vars["URL"].HasValue())
                    {
                        var ssl = vars["HTTP_X_FORWARDED_PROTO"] == "https" || vars["HTTP_X_SSL"].HasValue() || vars["HTTPS"] == "on";
                        var url = string.Format("http{3}://{0}{1}{2}", vars["HTTP_HOST"], vars["URL"], vars["QUERY_STRING"].HasValue() ? "?" + vars["QUERY_STRING"] : "", ssl ? "s" : "");
                        sb.AppendFormat("        <tr><td class=\"key\">URL and Query</td><td class=\"value\">{0}</td></tr>", vars["REQUEST_METHOD"] == "GET" ? Linkify(url) : url.HtmlEncode());
                    }
                    sb.Append(hiddenRows)
                      .AppendLine("      </table>")
                      .AppendLine("    </div>");
                }
                if (errored)
                {
                    sb.AppendFormat("<span class=\"custom-error-label\">Get {0} threw an exception:</span>", title)
                      .AppendFormat("<pre class=\"error-detail\">{0}</pre>", fetchError.HtmlEncode());
                }
                sb.AppendFormat("  </div>");
            }

            sb.AppendLine("<div id=\"ErrorInfo\">");
            if (Error == null)
            {
                sb.AppendFormat("  <h1 class=\"not-found\">Oh no! Error {0} was not found!</h1>", _guid.ToString()).AppendLine();
            }
            else
            {
                sb.Append("  <h1 class=\"error-title\">").AppendHtmlEncode(Error.Message).AppendLine("</h1>")
                  .Append("  <div class=\"error-type\">").AppendHtmlEncode(Error.Type).AppendLine("</div>")
                  .Append("  <pre class=\"error-detail\">").AppendHtmlEncode(Error.Detail).AppendLine().AppendLine("</pre>")
                  .Append("  <p class=\"error-time\">occurred <b title=\"")
                    .AppendHtmlEncode(Error.CreationDate.ToLongDateString()).Append(" at ").AppendHtmlEncode(Error.CreationDate.ToLongTimeString())
                    .Append("\">")
                    .Append(Error.CreationDate.ToRelativeTime())
                    .Append("</b> on ")
                    .Append(" <span class=\"info-delete-link\">(<a class=\"info-link\" href=\"delete?guid=").Append(Error.GUID.ToString()).AppendLine("\">delete</a>)</span></p>");
                if (Error.SQL.HasValue())
                {
                    sb.AppendLine("  <h3>SQL</h3>")
                      .AppendLine("  <pre class=\"sql-detail\">")
                      .AppendHtmlEncode(Error.SQL)
                      .AppendLine("</pre>")
                      .AppendLine("<br/>");
                }
                RenderVariableTable("Server Variables", "server-variables", Error.ServerVariables);

                if (Error.CustomData?.Count > 0)
                {
                    var errored = Error.CustomData.ContainsKey(Constants.CustomDataErrorKey);
                    var cdKeys = Error.CustomData.Keys.Where(k => k != Constants.CustomDataErrorKey);
                    sb.AppendLine("  <div class=\"custom-data\">");
                    if (errored)
                    {
                        sb.AppendLine("    <h3 class=\"kv-title title-error\">Custom - Error while gathering custom data</h3>");
                    }
                    else
                    {
                        sb.AppendLine("    <h3 class=\"kv-title\">Custom</h3>");
                    }
                    if (cdKeys.Any(k => k != Constants.CustomDataErrorKey))
                    {
                        sb.AppendLine("    <div class=\"side-scroll\">")
                          .AppendLine("      <table class=\"kv-table\">");
                        foreach (var cd in cdKeys)
                        {
                            sb.AppendLine("        <tr>")
                              .Append("          <td class=\"key\">").AppendHtmlEncode(cd).AppendLine("</td>")
                              .Append("          <td class=\"value\">").Append(Linkify(Error.CustomData[cd])).AppendLine("</td>")
                              .AppendLine("        </tr>");
                        }
                        sb.AppendLine("      </table>")
                          .AppendLine("    </div>");
                    }
                    if (errored)
                    {
                        sb.AppendLine("    <span class=\"custom-error-label\">GetCustomData threw an exception:</span>")
                          .AppendLine("    <pre class=\"error-detail\">").Append(Error.CustomData[Constants.CustomDataErrorKey]).AppendLine("</pre>");
                    }
                    sb.AppendLine("  </div>");
                }
                RenderVariableTable("QueryString", "querystring", Error.QueryString);
                RenderVariableTable("Form", "form", Error.Form);
                RenderVariableTable("Cookies", "cookies", Error.Cookies);
                RenderVariableTable("RequestHeaders", "headers", Error.RequestHeaders);
            }
            sb.AppendLine("</div>");
        }
    }
}