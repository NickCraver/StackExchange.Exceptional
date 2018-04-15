using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace StackExchange.Exceptional.Internal
{
    internal class ErrorEmail : HtmlBase
    {
        private readonly Error error;

        public ErrorEmail(Error error)
        {
           this.error = error;
        }

        protected override void RenderHtml(StringBuilder sb)
        {
            void RenderVariableTable(string title, NameValueCollection vars, bool renderUrls = false)
            {
                if (vars == null || vars.Count == 0) return;

                var fetchError = vars[Constants.CollectionErrorKey];
                var errored = fetchError.HasValue();
                var keys = vars.AllKeys.Where(key => !HiddenHttpKeys.Contains(key) && key != Constants.CollectionErrorKey).OrderBy(k => k);

                sb.AppendFormat("  <div>").AppendLine();
                sb.AppendFormat("    <h3 style=\"color: #224C00; font-family: Verdana, Tahoma, Arial, 'Helvetica Neue', Helvetica, sans-serif; font-size: 14px; margin: 10px 0 5px 0;\">{0}{1}</h3>", title, errored ? " - Error while gathering data" : "").AppendLine();
                if (keys.Any())
                {
                    sb.AppendFormat("    <table style=\"font-family: Verdana, Tahoma, Arial, 'Helvetica Neue', Helvetica, sans-serif; font-size: 12px; width: 100%; border-collapse: collapse; border: 0;\">").AppendLine();
                    var i = 0;
                    string getBackground() => i++ % 2 == 1 ? " style=\"background-color: #F2F2F2;\"" : "";

                    foreach (var k in keys)
                    {
                        // If this has no value, skip it
                        if (vars[k].IsNullOrEmpty() || DefaultHttpKeys.Contains(k))
                        {
                            continue;
                        }
                        sb.AppendFormat("      <tr{2}><td style=\"padding: 0.4em; width: 200px;\">{0}</td><td style=\"padding: 0.4em;\">{1}</td></tr>", k, Linkify(vars[k]), getBackground()).AppendLine();
                    }
                    if (renderUrls && vars["Request Method"].IsNullOrEmpty()) // told to render and we don't have them elsewhere
                    {
                        var method = error.HTTPMethod;
                        if (method.HasValue())
                        {
                            sb.AppendFormat("        <tr{1}><td style=\"padding: 0.4em; width: 200px;\">Method</td><td style=\"padding: 0.4em;\">{0}</td></tr>", method, getBackground()).AppendLine();
                            var fullUrl = error.GetFullUrl();
                            if (fullUrl.HasValue())
                            {
                                sb.AppendFormat("      <tr{1}><td style=\"padding: 0.4em; width: 200px;\">URL and Query</td><td style=\"padding: 0.4em;\">{0}</td></tr>", method == "GET" ? Linkify(fullUrl) : fullUrl.HtmlEncode(), getBackground()).AppendLine();
                            }
                        }
                    }
                    sb.AppendFormat("    </table>").AppendLine();
                }
                if (errored)
                {
                    sb.AppendFormat("    <span style=\"color: maroon;\">Get {0} threw an exception:</span>", title).AppendLine();
                    sb.AppendFormat("    <pre  style=\"background-color: #EEE; font-family: Consolas, Monaco, monospace; padding: 8px;\">{0}</pre>", fetchError.HtmlEncode()).AppendLine();
                }
                sb.AppendFormat("  </div>").AppendLine();
            }

            sb.AppendLine("<div style=\"font-family: Arial, \'Helvetica Neue\', Helvetica, sans-serif;\">");

            if (error == null)
            {
                sb.AppendLine("  <h1 style=\"color: maroon; font-size: 16px;\">Error not found.</h1>");
            }
            else
            {
                sb.Append("  <h1 style=\"color: maroon; font-size: 16px; padding: 0; margin: 0;\">")
                  .AppendHtmlEncode(error.Message)
                  .Append("</h1>").AppendLine()
                  .Append("  <div style=\"font-size: 12px; color: #444; padding: 0; margin: 2px 0;\">")
                  .AppendHtmlEncode(error.Type)
                  .Append("</div>").AppendLine()
                  .Append("  <pre style=\"background-color: #FFFFCC; font-family: Consolas, Monaco, monospace; font-size: 12px; margin: 2px 0; padding: 12px;\">")
                  .AppendHtmlEncode(error.Detail).AppendLine()
                  .Append("  </pre>").AppendLine()
                  .Append("  <p class=\"error-time\" style=\"font-size: 13px; color: #555; margin: 5px 0;\">occurred at <b title=\"")
                  .AppendHtmlEncode(error.CreationDate.ToLongDateString())
                  .Append(" at ")
                  .AppendHtmlEncode(error.CreationDate.ToLongTimeString())
                  .Append("\">")
                  .AppendHtmlEncode(error.CreationDate.ToUniversalTime().ToString())
                  .Append(" UTC</b> on ")
                  .AppendHtmlEncode(error.MachineName)
                  .Append("</p>")
                  .AppendLine();

                // TODO: Commands
                //if (!string.IsNullOrEmpty(error.SQL))
                //{
                //    sb.Append("  <h3 style=\"color: #224C00; font-family: Verdana, Tahoma, Arial, \'Helvetica Neue\', Helvetica, sans-serif; font-size: 14px; margin: 10px 0 5px 0;\">SQL</h3>")
                //      .AppendLine()
                //      .Append("  <pre style=\"background-color: #EEE; font-family: Consolas, Monaco, monospace; padding: 8px 8px 8px 8px; margin: 2px 0;\">")
                //      .AppendHtmlEncode(error.SQL)
                //      .Append("</pre>").AppendLine()
                //      .Append("<br/>").AppendLine();
                //}

                RenderVariableTable("Server Variables", error.ServerVariables, renderUrls: true);

                if (error.CustomData?.Count > 0)
                {
                    var errored = error.CustomData.ContainsKey(Constants.CustomDataErrorKey);
                    var cdKeys = error.CustomData.Keys.Where(k => k != Constants.CustomDataErrorKey);
                    sb.AppendLine("  <div class=\"custom-data\">");
                    if (errored)
                    {
                        sb.AppendLine("    <h3 style=\"color: maroon; font-family: Verdana, Tahoma, Arial, \'Helvetica Neue\', Helvetica, sans-serif; font-size: 14px; margin: 10px 0 5px 0;\">Custom - Error while gathering custom data</h3>");
                    }
                    else
                    {
                        sb.AppendLine("    <h3 style=\"color: #224C00; font-family: Verdana, Tahoma, Arial, \'Helvetica Neue\', Helvetica, sans-serif; font-size: 14px; margin: 10px 0 5px 0;\">Custom</h3>\r\n");
                    }

                    if (cdKeys.Any(k => k != Constants.CustomDataErrorKey))
                    {
                        var i = -1;
                        sb.AppendLine("     <table style=\"font-family: Verdana, Tahoma, Arial, \'Helvetica Neue\', Helvetica, sans-serif; font-size: 12px; width: 100%; border-collapse: collapse; border: 0;\">\r\n");

                        foreach (var cd in cdKeys)
                        {
                            i++;
                            sb.Append("      <tr");
                            if (i % 2 == 0) sb.Append(" style=\"background-color: #F2F2F2;\"");
                            sb.AppendLine(">")
                              .AppendLine("        <td style=\"padding: 0.4em; width: 200px;\">")
                              .AppendHtmlEncode(cd)
                              .AppendLine("</td>")
                              .Append("        <td style=\"padding: 0.4em;\">")
                              .Append(Linkify(error.CustomData[cd]))
                              .AppendLine("</td>")
                              .AppendLine("      </tr>");
                        }
                        sb.AppendLine("    </table>");
                    }

                    if (errored)
                    {
                        sb.AppendLine("    <span style=\"color: maroon;\">GetCustomData threw an exception:</span>")
                          .Append("    <pre style=\"background-color: #EEE; font-family: Consolas, Monaco, monospace; padding: 8px;\">")
                          .AppendHtmlEncode(error.CustomData[Constants.CustomDataErrorKey])
                          .AppendLine("</pre>");
                    }
                    sb.AppendLine("  </div>");
                }
                RenderVariableTable("QueryString", error.QueryString);
                RenderVariableTable("Form", error.Form);
                RenderVariableTable("Cookies", error.Cookies);
            }
            sb.AppendLine("</div>");
        }
    }
}
