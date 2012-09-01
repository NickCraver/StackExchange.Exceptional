using System.Web;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class ContentHandler : IHttpHandler
    {
        private readonly string _content;
        private readonly string _contentType;

        public ContentHandler(string message, string contentType)
        {
            _content = message;
            _contentType = contentType;
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = _contentType;
            context.Response.Write(_content);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}