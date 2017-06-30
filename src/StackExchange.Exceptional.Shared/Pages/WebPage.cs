using StackExchange.Exceptional.Internal;
using System;
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
        /// The title of the page.
        /// </summary>
        public string PageTitle { get; }
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
        public ExceptionalSettings Settings => ExceptionalSettings.Current;

        /// <summary>
        /// Creates a new <see cref="WebPage"/> for rendering.
        /// </summary>
        /// <param name="error">The current error (null if not on an error-specific page).</param>
        /// <param name="store">The store to render.</param>
        /// <param name="baseURL">The base URL for the current request.</param>
        /// <param name="pageTitle">The title of the page.</param>
        protected WebPage(Error error, ErrorStore store, string baseURL, string pageTitle)
        {
            Error = error;
            Store = store;
            BaseUrl = baseURL.EndsWith("/") ? baseURL : baseURL + "/";
            PageTitle = pageTitle;
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
              .AppendLine("<html>")
              .AppendLine("  <head>")
              .AppendFormat("    <title>{0}</title>", PageTitle.HtmlEncode()).AppendLine()
              .AppendFormat("    <link rel=\"stylesheet\" type=\"text/css\" href=\"{0}\" />", Url("css")).AppendLine();

            foreach (var css in Settings.Render.CSSIncludes)
            {
                sb.AppendFormat("    <link rel=\"stylesheet\" type=\"text/css\" href=\"{0}\" />", css).AppendLine();
            }

            sb.AppendFormat("    <script>var baseUrl = '{0}';</script>", Url("")).AppendLine();
            if (Error != null)
            {
                sb.Append("      <script>var Exception = ");
                sb.Append(Error.ToJson());
                //Error.WriteDetailedJson(sb);
                sb.AppendLine(";</script>");
            }

            sb.AppendFormat("    <script src=\"{0}\"></script>", Settings.Render.JQueryURL).AppendLine()
              .AppendFormat("    <script src=\"{0}\"></script>", Url("js")).AppendLine();
            foreach (var js in Settings.Render.JSIncludes)
            {
                sb.AppendFormat("    <script src=\"{0}\"></script>", js).AppendLine();
            }
            sb.AppendLine("  </head>")
              .AppendLine("  <body>")
              .AppendLine("    <div class=\"wrapper\">")
              .AppendFormat("      <header{0}>Exceptions Log: {1}</header>", Store.InFailureMode ? "class=\"failure\"" : "", Settings.ApplicationName.HtmlEncode()).AppendLine()
              .AppendLine("      <section id=\"content\">");

            // Render the page inheriting from us
            RenderInnerHtml(sb);
            sb.AppendLine()
              .AppendLine("      </section>")
              .AppendLine("      <div class=\"bottom\"></div>")
              .AppendLine("    </div>")
              .AppendLine("    <footer>")
              .AppendLine("      <div class=\"version-info\">Exceptional ").Append(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)
              .Append("<br/>")
              .AppendHtmlEncode(Store.Name).AppendLine()
              .AppendLine("      </div>")
              .AppendLine("      <div class=\"server-time\">Server time is ").Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")).AppendLine("</div>")
              .AppendLine("    </footer>")
              .AppendLine("  </body>")
              .AppendLine("</html>");
        }
    }
}