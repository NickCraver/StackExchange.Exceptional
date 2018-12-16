using StackExchange.Exceptional.Internal;
using System;
using System.Reflection;
using System.Text;

namespace StackExchange.Exceptional.Pages
{
    /// <summary>
    /// Represents a base page with the chrome for rendering HTML content.
    /// Override this page and implement <see cref="RenderInnerHtml(StringBuilder)"/> for rendering.
    /// </summary>
    public abstract class WebPage : HtmlBase
    {
        /// <summary>
        /// The current Error, if any.
        /// </summary>
        public Error Error { get; }
        /// <summary>
        /// The &gt;title&lt; of the page.
        /// </summary>
        public string PageTitle { get; set; }
        /// <summary>
        /// The header title, visible in the page itself.
        /// </summary>
        public string HeaderTitle { get; set; }
        /// <summary>
        /// The store we're rendering with this page.
        /// </summary>
        public ErrorStore Store { get; }
        /// <summary>
        /// The base URL for requests to this page.
        /// </summary>
        private string BaseUrl { get; }
        /// <summary>
        /// The current settings.
        /// </summary>
        public ExceptionalSettingsBase Settings { get; }

        /// <summary>
        /// Whether to inline CSS styles in the page.
        /// </summary>
        public bool InlineCSS { get; set; } = false;
        /// <summary>
        /// Whether to include the JS as a linked resource.
        /// </summary>
        public bool IncludeJS { get; set; } = true;

        /// <summary>
        /// Creates a new <see cref="WebPage"/> for rendering.
        /// </summary>
        /// <param name="error">The current error (null if not on an error-specific page).</param>
        /// <param name="settings">Current Exceptional settings.</param>
        /// <param name="store">The store to render.</param>
        /// <param name="baseURL">The base URL for the current request.</param>
        /// <param name="pageTitle">The title of the page.</param>
        protected WebPage(Error error, ExceptionalSettingsBase settings, ErrorStore store, string baseURL, string pageTitle)
        {
            Error = error;
            Settings = settings;
            Store = store;
            BaseUrl = baseURL.EndsWith("/") ? baseURL : baseURL + "/";
            PageTitle = pageTitle;
            HeaderTitle = "Exceptions Log: " + settings.Store.ApplicationName.HtmlEncode();
        }

        /// <summary>
        /// Gets the full URL for a given path.
        /// </summary>
        /// <param name="path">The path to get a full URL for.</param>
        protected string Url(string path) => BaseUrl + path;

        /// <summary>
        /// The method to override for rendering the main contents of the page.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to render to.</param>
        protected abstract void RenderInnerHtml(StringBuilder sb);

        /// <summary>
        /// Renders the page to the provided <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to render to.</param>
        protected override void RenderHtml(StringBuilder sb)
        {
            sb.AppendLine("<!DOCTYPE html>")
              .AppendLine("<meta charset=\"utf-8\">")
              .AppendLine("<html>")
              .AppendLine("  <head>")
              .AppendFormat("    <title>{0}</title>", PageTitle.HtmlEncode()).AppendLine();
            if (InlineCSS)
            {
                sb.Append("    <style>").Append(Resources.BundleCss.Content).AppendLine("</style>");
            }
            else
            {
                sb.AppendFormat("    <link rel=\"stylesheet\" type=\"text/css\" href=\"{0}?v={1}\" integrity=\"sha512-{1}\" crossorigin=\"anonymous\"/>", Url(KnownRoutes.Css), Resources.BundleCss.Sha512).AppendLine();
            }

            foreach (var css in Settings.Render.CSSIncludes)
            {
                sb.AppendFormat("    <link rel=\"stylesheet\" type=\"text/css\" href=\"{0}\" />", css).AppendLine();
            }

            sb.AppendFormat("    <script>var baseUrl = '{0}';</script>", Url("")).AppendLine();
            if (Error != null)
            {
                sb.Append("    <script>var Exception = ");
                Error.WriteDetailedJson(sb);
                sb.AppendLine(";</script>");
            }

            if (IncludeJS)
            {
                sb.AppendFormat("    <script src=\"{0}?v={1}\" integrity=\"sha512-{1}\" crossorigin=\"anonymous\"></script>", Url(KnownRoutes.Js), Resources.BundleJs.Sha512).AppendLine();
            }
            foreach (var js in Settings.Render.JSIncludes)
            {
                sb.AppendFormat("    <script src=\"{0}\"></script>", js).AppendLine();
            }
            sb.AppendLine("  </head>")
              .AppendLine("  <body>")
              .AppendLine("    <div class=\"wrapper\">")
              .AppendFormat("      <header{0}>{1}</header>", Store.InFailureMode ? " class=\"failure\"" : "", HeaderTitle).AppendLine()
              .AppendLine("      <main>");

            // Render the page inheriting from us
            RenderInnerHtml(sb);
            sb.AppendLine("      </main>")
              .AppendLine("      <div class=\"bottom\"></div>")
              .AppendLine("    </div>")
              .AppendLine("    <footer>")
              .Append("      <div class=\"version-info\">Exceptional ").Append(typeof(Error).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion)
              .Append("<br/>")
              .AppendHtmlEncode(Store.Name).AppendLine("</div>")
              .Append("      <div class=\"server-time\">Server time is ").Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")).AppendLine("</div>")
              .AppendLine("    </footer>")
              .AppendLine("  </body>")
              .AppendLine("</html>");
        }

        // The icons below are from Font Awesome by Dave Gandy
        // Home: http://fontawesome.io/
        // License: http://fontawesome.io/license/
        // Port project: https://github.com/encharm/Font-Awesome-SVG-PNG

        /// <summary>
        /// SVG Icon: X
        /// </summary>
        protected const string IconX = "<svg class='icon x' viewBox='0 0 1792 1792'><path d='M1490 1322q0 40-28 68l-136 136q-28 28-68 28t-68-28l-294-294-294 294q-28 28-68 28t-68-28l-136-136q-28-28-28-68t28-68l294-294-294-294q-28-28-28-68t28-68l136-136q28-28 68-28t68 28l294 294 294-294q28-28 68-28t68 28l136 136q28 28 28 68t-28 68l-294 294 294 294q28 28 28 68z'/></svg>";

        /// <summary>
        /// SVG Icon: Lock
        /// </summary>
        protected const string IconLock = "<svg class='icon lock' viewBox='0 0 1792 1792'><path d='M640 768h512v-192q0-106-75-181t-181-75-181 75-75 181v192zm832 96v576q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-576q0-40 28-68t68-28h32v-192q0-184 132-316t316-132 316 132 132 316v192h32q40 0 68 28t28 68z'/></svg>";
    }
}
