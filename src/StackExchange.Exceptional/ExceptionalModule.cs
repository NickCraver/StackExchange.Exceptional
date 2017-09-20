using System;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP module that catches and log exceptions from ASP.NET Applications.
    /// </summary>
    public class ExceptionalModule : IHttpModule
    {
        static ExceptionalModule() => Settings.LoadSettings();

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>
        /// <param name="context">The <see cref="HttpApplication"/> we're running in.</param>
        public virtual void Init(HttpApplication context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context), "Could not find HttpApplication");
            context.Error += OnError;
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module.
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// The handler called when an unhandled exception bubbles up to the module.
        /// </summary>
        /// <param name="sender">The source of the error.</param>
        /// <param name="args">The error arguments.</param>
        protected virtual void OnError(object sender, EventArgs args)
        {
            var app = (HttpApplication)sender;
            app.Server.GetLastError()?.Log(app.Context);
        }

        /// <summary>
        /// Convenience method for handling a request, for usage in your routing, MVC, etc. See example.
        /// </summary>
        /// <example>
        /// <code>
        /// [Route("/path/my-route")]
        /// public Task Exceptions() => ExceptionalModule.HandleRequestAsync(System.Web.HttpContext.Current);
        /// </code>
        /// </example>
        /// <param name="context">The context to process, usually System.Web.HttpContext.Current.</param>
        /// <returns>A task to await.</returns>
        public static async Task HandleRequestAsync(HttpContext context)
        {
            var page = new HandlerFactory().GetHandler(context, context.Request.RequestType, context.Request.Url.ToString(), context.Request.PathInfo);
            if (page is HttpTaskAsyncHandler apage)
            {
                await apage.ProcessRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                page.ProcessRequest(context);
            }
        }
    }
}