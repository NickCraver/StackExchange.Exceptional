using System.Web;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class RedirectHandler : IHttpHandler
    {
        private string _url;
        private bool _redirectIfAjax;

        public RedirectHandler(string url, bool redirectIfAjax)
        {
            _url = url;
            _redirectIfAjax = redirectIfAjax;
        }

        public void ProcessRequest(HttpContext context)
        {
            if (!_redirectIfAjax && context.Request.Headers["X-Requested-With"] == "XMLHttpRequest") return;
            context.Response.Redirect(_url);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}