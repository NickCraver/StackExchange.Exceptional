using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP handler that chooses the correct handler/view based on the request.
    /// </summary>
    public class HandlerFactory : IHttpHandlerFactory
    {
        static HandlerFactory() => Settings.LoadSettings();

        /// <summary>
        /// Gets the HttpHandler for executing the request, used to proxy requests through here (e.g. MVC) or by the HttpModule directly.
        /// </summary>
        /// <param name="context">The HTTPContext for the request.</param>
        /// <param name="requestType">The type of request, GET/POST.</param>
        /// <param name="url">The URL of the request.</param>
        /// <param name="pathTranslated">The translated path of the request.</param>
        /// <returns>The HTTPHandler that can execute the request.</returns>
        public virtual IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated) =>
            new ExceptionalAsyncHandler(url);

        /// <summary>
        /// Enables the factory to reuse an existing handler instance.
        /// </summary>
        /// <param name="handler">The handler to release.</param>
        public virtual void ReleaseHandler(IHttpHandler handler) { }
    }
}
