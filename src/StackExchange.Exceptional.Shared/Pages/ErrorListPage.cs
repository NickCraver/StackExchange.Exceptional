using StackExchange.Exceptional.Internal;
using System.Linq;
using System.Text;

namespace StackExchange.Exceptional.Pages
{
    /// <summary>
    /// An error listing page for showing the last n errors in the store.
    /// </summary>
    public class ErrorListPage : WebPage
    {
        /// <summary>
        /// Creates an error listing page for rendering.
        /// </summary>
        /// <param name="store">The error store to use.</param>
        /// <param name="baseURL">The base URL for all links and items in the page.</param>
        public ErrorListPage(ErrorStore store, string baseURL)
            : base(null, store, baseURL, "Error Log") { }

        /// <summary>
        /// Renders the contents of the middle of the master page.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to render to.</param>
        protected override void RenderInnerHtml(StringBuilder sb)
        {
            var errors = Store.GetAll();
            var total = errors.Count;
            errors = errors.OrderByDescending(e => e.CreationDate).ToList();

            if (Store.InFailureMode)
            {
                sb.AppendLine("    <div class=\"failure-mode\">Error log is in failure mode, ")
                  .Append(ErrorStore.WriteQueue.Count)
                  .Append(" ")
                  .Append(ErrorStore.WriteQueue.Count == 1 ? "entry" : "entries")
                  .Append(" queued to log.");

                var le = Store.LastRetryException;
                if (le != null)
                {
                    sb.Append("<br />Last Logging Exception: ").AppendHtmlEncode(le.Message)
                      .AppendLine("    </div>")
                      .AppendLine("<!-- Exception Details:")
                      .AppendHtmlEncode(le.Message)
                      .AppendLine()
                      .AppendHtmlEncode(le.StackTrace)
                      .AppendLine()
                      .AppendLine("-->");
                }
            }
            if (errors.Count == 0)
            {
                sb.AppendLine("    <div class=\"empty\">")
                  .AppendLine("      <h1>No errors yet, yay!</h1>")
                  .AppendLine("      <div>There are no active errors in the log.</div>")
                  .AppendLine("    </div>");
            }
            else
            {
                var last = errors.FirstOrDefault(); // oh the irony
                sb.AppendLine("    <h1 id=\"errorcount\">")
                  .Append(total)
                  .Append(" Error")
                  .Append(total > 1 ? "s" : null)
                  .Append(" <span>(last: ")
                  .AppendHtmlEncode(last.CreationDate.ToRelativeTime())
                  .AppendLine(")</span></h1>")
                  .Append(@"    <table id=""ErrorLog"" class=""alt-rows"">
      <thead>
        <tr>
          <th class=""type-col"">&nbsp;</th>
          <th class=""type-col"">Type</th>
          <th>Error</th>
          <th>Url</th>
          <th>Remote IP</th>
          <th>Time</th>
          <th>Site</th>
          <th>Server</th>
        </tr>
      </thead>
      <tbody>");
                foreach (var e in errors)
                {
                    sb.Append("        <tr data-id=\"").Append(e.GUID.ToString()).Append("\" class=\"error").Append(e.IsProtected ? " protected" : "").AppendLine("\">")
                      .Append("          <td>")
                      .Append("<a href=\"#\" data-url=\"").Append(Url("delete")).AppendLine("\" class=\"delete-link js-delete-link\" title=\"Delete this error\">&nbsp;X&nbsp;</a>");
                    if (!e.IsProtected)
                    {
                        sb.Append(" <a href=\"#\" data-url=\"").Append(Url("protect")).Append("\" class=\"protect-link js-protect-link\" title=\"Protect this error\">&nbsp;P&nbsp;</a>");
                    }
                    sb.AppendLine("</td>")
                      .Append("          <td class=\"type-col\" title=\"").AppendHtmlEncode(e.Type).Append("\">")
                      .AppendHtmlEncode(e.Type.ToShortException())
                      .AppendLine("</td>")
                      .Append("          <td class=\"error-col\"><a href=\"").Append(Url("info?guid=" + e.GUID.ToString()))
                      .Append("\" class=\"info-link\">").AppendHtmlEncode(e.Message).Append("</a>");
                    if (e.DuplicateCount > 1)
                    {
                        sb.Append(" <span class=\"duplicate-count\" title=\"number of similar errors occurring close to this error\">(")
                          .Append(e.DuplicateCount.ToString())
                          .Append(")</span>");
                    }
                    sb.AppendLine("</td>")
                      .Append("          <td>");
                    if (e.Url.HasValue())
                    {
                        sb.Append("<span title=\"").AppendHtmlEncode(e.Host).AppendHtmlEncode(e.Url).Append("\">")
                          .AppendHtmlEncode(e.Url.TruncateWithEllipsis(40))
                          .Append("</span>");
                    }
                    sb.AppendLine("</td>")
                      .Append("          <td>").AppendHtmlEncode(e.IPAddress).AppendLine("</td>")
                      .Append("          <td><span title=\"")
                      .Append(e.CreationDate.ToUniversalTime().ToString("u"))
                      .Append("\">")
                      .AppendHtmlEncode(e.CreationDate.ToRelativeTime())
                      .AppendLine("</span></td>")
                      .Append("          <td>").AppendHtmlEncode(e.Host).AppendLine("</td>")
                      .Append("          <td>").AppendHtmlEncode(e.MachineName).AppendLine("</td>")
                      .AppendLine("        </tr>");
                }
                sb.AppendLine("      </tbody>")
                  .AppendLine("    </table>");
                if (errors.Any(e => !e.IsProtected))
                {
                    sb.Append("    <div class=\"clear-all-div\">")
                      .Append("<a class=\"delete-link js-delete-link clear-all-link\" href=\"#\" data-url=\"")
                      .Append(Url("delete-all"))
                      .Append("\">&nbsp;X&nbsp;- Clear all non-protected errors</a>")
                      .AppendLine("</div>");
                }
            }
        }
    }
}