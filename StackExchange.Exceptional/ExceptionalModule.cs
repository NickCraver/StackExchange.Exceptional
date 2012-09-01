using System;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP module that catches and log exceptions from ASP.Net Applications
    /// </summary>   
    public class ExceptionalModule : IHttpModule
    {
        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>
        public virtual void Init(HttpApplication app)
        {
            if (app == null) throw new ArgumentNullException("app", "Could not find HttpApplication");
            app.Error += OnError;
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module.
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// Gets the <see cref="ErrorStore"/> instance to which the module will log exceptions.
        /// </summary>        
        public virtual ErrorStore ErrorStore
        {
            get { return ErrorStore.Default; }
        }

        /// <summary>
        /// The handler called when an unhandled exception bubbles up to the module.
        /// </summary>
        protected virtual void OnError(object sender, EventArgs args)
        {
            var app = (HttpApplication)sender;
            var ex = app.Server.GetLastError();

            LogException(ex, app.Context);
        }

        /// <summary>
        /// Logs an exception and its context to the error log.
        /// </summary>
        public virtual void LogException(Exception ex, HttpContext context, bool appendFullStackTrace = false)
        {
            ErrorStore.LogException(ex, context, appendFullStackTrace);
        }
    }
}