using System.Collections.Specialized;
using System.Linq;
using System.Text;
using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;

namespace StackExchange.Exceptional.Pages
{
    /// <summary>
    /// An error detail page for showing the specifics on a single error.
    /// </summary>
    public class ErrorDetailPage : WebPage
    {
        private readonly Guid _guid;

        /// <summary>
        /// Whether to show the action links for this exception.
        /// </summary>
        public bool ShowActionLinks { get; set; }

        /// <summary>
        /// Creates an <see cref="ErrorDetailPage"/>.
        /// </summary>
        /// <param name="error">The error we're rendering details.</param>
        /// <param name="settings">Current Exceptional settings.</param>
        /// <param name="store">The store this error is from.</param>
        /// <param name="baseURL">The base URL for the current request.</param>
        /// <param name="guid">The id for the error (populate even if the error is null).</param>
        public ErrorDetailPage(Error error, ExceptionalSettingsBase settings, ErrorStore store, string baseURL, Guid guid)
            : base(error, settings, store, baseURL, "Error - " + (error?.Message ?? "Not Found"))
        {
            _guid = guid;
        }

        /// <summary>
        /// Renders the main contents of the page.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to render to.</param>
        protected override void RenderInnerHtml(StringBuilder sb)
        {
            void RenderVariableTable(string title, string className, NameValueCollection vars, bool renderUrls = false)
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
                    if (renderUrls && vars["Request Method"].IsNullOrEmpty()) // told to render and we don't have them elsewhere
                    {
                        var method = Error.HTTPMethod;
                        if (method.HasValue())
                        {
                            sb.AppendFormat("        <tr><td>Method</td><td>{0}</td></tr>", method).AppendLine();
                            var fullUrl = Error.GetFullUrl();
                            if (fullUrl.HasValue())
                            {
                                sb.AppendFormat("        <tr><td>URL and Query</td><td>{0}</td></tr>", method == "GET" ? Linkify(fullUrl) : fullUrl.HtmlEncode()).AppendLine();
                            }
                        }
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

            void RenderKeyValueTable(IEnumerable<KeyValuePair<string, string>> kvPairs)
            {
                if (kvPairs?.Any() == true)
                {
                    sb.AppendLine("    <div class=\"side-scroll\">")
                      .AppendLine("      <table class=\"alt-rows key-value\">");
                    foreach (var kv in kvPairs)
                    {
                        sb.Append("        <tr>")
                          .Append("<td>").AppendHtmlEncode(kv.Key).Append("</td>")
                          .Append("<td>").Append(Linkify(kv.Value)).Append("</td>")
                          .AppendLine("</tr>");
                    }
                    sb.AppendLine("      </table>")
                      .AppendLine("    </div>");
                }
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
                  .Append("  <pre class=\"stack dark\"><code class=\"nohighlight\">").AppendLine(ExceptionalUtils.StackTrace.HtmlPrettify(Error.Detail, Settings.StackTrace)).AppendLine("</code></pre>")
                  // TODO: Controls for show/hide of async .stack.row.async in the block above
                  // TODO: Remove - temporarily showing the raw while the user-friendlier display above gets tuned
                  //.Append("  <pre class=\"stack\"><code>").AppendHtmlEncode(Error.Detail).AppendLine().AppendLine("</code></pre>")
                  .Append("  <p class=\"sub-info\">occurred <b title=\"")
                  .AppendHtmlEncode(Error.CreationDate.ToLongDateString()).Append(" at ").AppendHtmlEncode(Error.CreationDate.ToLongTimeString())
                  .Append("\">")
                  .Append(Error.CreationDate.ToRelativeTime())
                  .Append("</b> on ")
                  .AppendHtmlEncode(Error.MachineName);
                if (ShowActionLinks)
                {
                    sb.Append(" <span>(<a href=\"delete?guid=").Append(Error.GUID.ToString()).AppendLine("\">delete</a>)</span>");
                }
                sb.Append("</p>");
                if (Error.Commands != null)
                {
                    foreach (var cmd in Error.Commands)
                    {
                        var lang = cmd.GetHighlightLanguage();
                        sb.Append("  <h3>Command: ").AppendHtmlEncode(cmd.Type).AppendLine("</h3>")
                          .Append("  <pre class=\"command\"><code");
                        if (lang.HasValue())
                        {
                            sb.Append(" class=\"").Append(lang).Append("\"");
                        }
                        sb.Append(">")
                          .AppendHtmlEncode(cmd.CommandString)
                          .AppendLine("</code></pre>");

                        RenderKeyValueTable(cmd.Data);
                    }
                }
                RenderVariableTable("Server Variables", "server-variables", Error.ServerVariables, renderUrls: true);

                if (Error.CustomData?.Count > 0)
                {
                    var errored = Error.CustomData.ContainsKey(Constants.CustomDataErrorKey);
                    var cdKVs = Error.CustomData.Where(kv => kv.Key != Constants.CustomDataErrorKey);
                    sb.AppendLine("  <div class=\"custom-data\">");
                    if (errored)
                    {
                        sb.AppendLine("    <h3 class=\"title-error\">Custom - Error while gathering custom data</h3>");
                    }
                    else
                    {
                        sb.AppendLine("    <h3>Custom</h3>");
                    }
                    if (cdKVs.Any())
                    {
                        RenderKeyValueTable(cdKVs);
                    }
                    if (errored)
                    {
                        sb.AppendLine("    <span class=\"custom-error-label\">GetCustomData threw an exception:</span>")
                          .AppendLine("    <pre class=\"stack\"><code>").Append(Error.CustomData[Constants.CustomDataErrorKey]).AppendLine("</code></pre>");
                    }
                    sb.AppendLine("  </div>");
                }
                RenderVariableTable("Querystring", "querystring", Error.QueryString);
                RenderVariableTable("Form", "form", Error.Form);
                RenderVariableTable("Cookies", "cookies", Error.Cookies);
                RenderVariableTable("Request Headers", "headers", Error.RequestHeaders);
            }
        }
    }
}
