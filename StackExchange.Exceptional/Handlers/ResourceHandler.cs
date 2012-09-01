using System.Net;
using System.Reflection;
using System.Web;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class ResourceHandler : IHttpHandler
    {
        private readonly string _resourceName;
        private readonly string _contentType;

        public ResourceHandler(string resourceName, string contentType)
        {
            _resourceName = resourceName;
            _contentType = contentType;
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = _contentType;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StackExchange.Exceptional.Resources." + _resourceName))
            {
                if (stream != null)
                {
                    stream.CopyTo(context.Response.OutputStream);
                }
                else
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    context.Response.Write("404");
                }
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}