using System.Text;
using System.Web;

namespace StackExchange.Exceptional
{
    internal class RazorPageBase : IHttpHandler
    {
        private readonly StringBuilder _output = new StringBuilder();
        string _content;
        public RazorPageBase Layout { get; set; }

        public HttpApplicationState Application { get { return HttpContext.Current.Application; } }
        public HttpResponse Response { get { return HttpContext.Current.Response; } }
        public HttpRequest Request { get { return HttpContext.Current.Request; } }
        public HttpServerUtility Server { get { return HttpContext.Current.Server; } }

        protected string BasePageName
        {
            get { return Request.ServerVariables["URL"]; }
        }

        public string Url(string path)
        {
            return BasePageName.EndsWith("/") ? BasePageName + path : BasePageName + "/" + path;
        }

        public IHtmlString Html(string html)
        {
            return new HtmlString(html);
        }

        public string AttributeEncode(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : HttpUtility.HtmlAttributeEncode(text);
        }

        public string Encode(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty  : HttpUtility.HtmlEncode(text);
        }

        public void Write(object value)
        {
            if (value != null)
            {
                var html = value as IHtmlString;
                _output.Append(html != null ? html.ToHtmlString() : Encode(value.ToString()));
            }
        }

        public virtual void Execute() { }

        public void WriteLiteral(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;
            _output.Append(textToAppend);
        }

        public object RenderBody()
        {
            return new HtmlString(_content);
        }

        public string TransformText()
        {
            Execute();

            if (Layout != null)
            {
                Layout._content = _output.ToString();
                return Layout.TransformText();
            }

            return _output.ToString();
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            ProcessRequest();
        }

        protected virtual void ProcessRequest()
        {
            Response.Write(TransformText());
        }

        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }
    }
}
