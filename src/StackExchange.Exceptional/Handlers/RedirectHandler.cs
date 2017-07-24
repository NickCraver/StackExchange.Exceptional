using System.Web;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class RedirectHandler : IHttpHandler
    {
        private readonly string _url;

        public RedirectHandler(string url) => _url = url;

        public void ProcessRequest(HttpContext context) => context.Response.Redirect(_url);

        public bool IsReusable => false;
    }
}