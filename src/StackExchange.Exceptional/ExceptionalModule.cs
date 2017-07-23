using System;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// HTTP module that catches and log exceptions from ASP.NET Applications.
    /// </summary>
    public class ExceptionalModule : IHttpModule
    {
        static ExceptionalModule() => ConfigSettings.LoadSettings();

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
        /// Gets the <see cref="ErrorStore"/> instance to which the module will log exceptions.
        /// </summary>        
        public virtual ErrorStore ErrorStore => ErrorStore.Default;

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
    }
}