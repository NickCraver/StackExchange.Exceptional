using StackExchange.Exceptional.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Exceptional.Pages
{
    /// <summary>
    /// An error listing page for showing the last n errors in the store.
    /// </summary>
    public class ErrorListPage : WebPage
    {
        private List<Error> Errors { get; }

        /// <summary>
        /// Creates an error listing page for rendering.
        /// </summary>
        /// <param name="store">The error store to use.</param>
        /// <param name="settings">Current Exceptional settings.</param>
        /// <param name="baseURL">The base URL for all links and items in the page.</param>
        /// <param name="errors">The list of errors to display on this page.</param>
        public ErrorListPage(ErrorStore store, ExceptionalSettingsBase settings, string baseURL, List<Error> errors)
            : base(null, settings, store, baseURL, "Error Log")
        {
            Errors = errors.OrderByDescending(e => e.LastLogDate ?? e.CreationDate).ToList();
        }

        /// <summary>
        /// Renders the contents of the middle of the master page.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to render to.</param>
        protected override void RenderInnerHtml(StringBuilder sb)
        {
            var total = Errors.Count;

            if (Store.InFailureMode)
            {
                sb.Append("        <div class=\"failure-mode\">")
                  // From https://github.com/encharm/Font-Awesome-SVG-PNG by https://github.com/Rush
                  // MIT License: https://github.com/encharm/Font-Awesome-SVG-PNG/blob/master/LICENSE
                  .Append("<svg viewBox=\"0 0 1792 1792\"><path d=\"M1024 1375v-190q0-14-9.5-23.5t-22.5-9.5h-192q-13 0-22.5 9.5t-9.5 23.5v190q0 14 9.5 23.5t22.5 9.5h192q13 0 22.5-9.5t9.5-23.5zm-2-374l18-459q0-12-10-19-13-11-24-11h-220q-11 0-24 11-10 7-10 21l17 457q0 10 10 16.5t24 6.5h185q14 0 23.5-6.5t10.5-16.5zm-14-934l768 1408q35 63-2 126-17 29-46.5 46t-63.5 17h-1536q-34 0-63.5-17t-46.5-46q-37-63-2-126l768-1408q17-31 47-49t65-18 65 18 47 49z\"/></svg>")
                  .Append("Error log is in failure mode, ")
                  .Append(Store.WriteQueue.Count)
                  .Append(" ")
                  .Append(Store.WriteQueue.Count == 1 ? "entry" : "entries")
                  .Append(" queued to log.");

                var le = Store.LastRetryException;
                if (le != null)
                {
                    sb.Append("<div>Last Logging Exception: ").AppendHtmlEncode(le.Message).AppendLine(" (<a href=\"#\" class=\"js-show-details\">view details</a>)</div>")
                      .Append("<pre class=\"stack dark details\"><code>")
                      .Append(ExceptionalUtils.StackTrace.HtmlPrettify(le.Message + "\n" + le.StackTrace, Settings.StackTrace))
                      .AppendLine("</code></pre>");
                }
                sb.AppendLine("</div>");
            }
            if (total == 0)
            {
                sb.AppendLine("        <div class=\"empty\">")
                  .AppendLine("          <h1>No errors yet, yay!</h1>")
                  .AppendLine("          <div>There are no active errors in the log.</div>")
                  .AppendLine("        </div>");
            }
            else
            {
                var last = Errors.FirstOrDefault(); // oh the irony
                sb.Append("        <h1>")
                  .Append("<span class=\"js-error-count\">")
                  .Append(total)
                  .Append(" Error")
                  .Append(total > 1 ? "s" : null)
                  .Append("</span>")
                  .Append(" <span class=\"sub\">(last: ")
                  .AppendHtmlEncode(last.CreationDate.ToRelativeTime())
                  .AppendLine(")</span></h1>")
                  .AppendLine(@"        <table class=""js-error-list hover alt-rows error-list"">
          <thead>
            <tr>
              <th></th>
              <th>Type</th>
              <th>Error</th>
              <th>Url</th>
              <th>Remote IP</th>
              <th>Time</th>
              <th>Site</th>
              <th>Server</th>
            </tr>
          </thead>
          <tbody>");
                foreach (var e in Errors)
                {
                    sb.Append("            <tr data-id=\"").Append(e.GUID.ToString()).Append("\" class=\"error").Append(e.IsProtected ? " js-protected" : "").AppendLine("\">")
                      .Append("              <td>")
                      .Append("<a href=\"#\" class=\"js-delete-link\" title=\"Delete this error\">").Append(IconX).Append("</a>");
                    if (!e.IsProtected)
                    {
                        sb.Append(" <a href=\"#\" class=\"js-protect-link\" title=\"Protect this error\">").Append(IconLock).Append("</a>");
                    }
                    else
                    {
                        sb.Append(" <span title=\"This error is protected\">").Append(IconLock).Append("</span>");
                    }
                    sb.AppendLine("</td>")
                      .Append("              <td title=\"").AppendHtmlEncode(e.Type).Append("\">")
                      .AppendHtmlEncode(e.Type.ToShortTypeName())
                      .AppendLine("</td>")
                      .Append("              <td class=\"wrap\"><a href=\"").Append(Url(KnownRoutes.Info)).Append("?guid=").Append(e.GUID.ToString())
                      .Append("\">").Append(e.Message.HasValue() ? e.Message.EncodeTruncateWithEllipsis(250) : "(no message)").Append("</a>");
                    if (e.DuplicateCount > 1)
                    {
                        sb.Append(" <span class=\"duplicate-count\" title=\"number of similar errors occurring close to this error\">(")
                          .Append(e.DuplicateCount.ToString())
                          .Append(")</span>");
                    }
                    sb.AppendLine("</td>")
                      .Append("              <td>");
                    if (e.UrlPath.HasValue())
                    {
                        sb.Append("<span title=\"").AppendHtmlEncode(e.Host).AppendHtmlEncode(e.UrlPath).Append("\">")
                          .Append(e.UrlPath.EncodeTruncateWithEllipsis(40))
                          .Append("</span>");
                    }
                    sb.AppendLine("</td>")
                      .Append("              <td>").AppendHtmlEncode(e.IPAddress).AppendLine("</td>")
                      .Append("              <td title=\"")
                      .Append((e.LastLogDate ?? e.CreationDate).ToUniversalTime().ToString("u"))
                      .Append("\">")
                      .AppendHtmlEncode((e.LastLogDate ?? e.CreationDate).ToRelativeTime())
                      .AppendLine("</td>")
                      .Append("              <td>").AppendHtmlEncode(e.Host).AppendLine("</td>")
                      .Append("              <td>").AppendHtmlEncode(e.MachineName).AppendLine("</td>")
                      .AppendLine("            </tr>");
                }
                sb.AppendLine("          </tbody>")
                  .AppendLine("        </table>");
                if (Errors.Any(e => !e.IsProtected))
                {
                    sb.Append("        <div class=\"page-actions\">")
                      .Append("<a class=\"js-clear-all\" href=\"#\">")
                      .Append(IconX)
                      .Append(" Clear all non-protected errors</a>")
                      .AppendLine("</div>");
                }
            }
        }
    }
}