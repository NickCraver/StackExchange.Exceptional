using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using StackExchange.Exceptional;

namespace Samples.MVC4
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Instead of any web.config entries, you can setup entirely through code
            // Setup Exceptional:
            // memory example:
            //ErrorStore.Setup("My Error Log Name", new MemoryErrorStore());
            // JSON example
            //ErrorStore.Setup("My Error Log Name", new JSONErrorStore(path: "~/Errors"));
            // SQL Example
            //ErrorStore.Setup("My Error Log Name", new SQLErrorStore(connectionString: "Data Source=.;Initial Catalog=Exceptions;Integrated Security=SSPI;"));

            // Optionally add custom data to any logged exception (visible on the exception detail page):
            ErrorStore.GetCustomData = (exception, context, data) =>
                {
                    // exception is the exception thrown
                    // context is the HttpContext of the request (could be null, e.g. background thread exception)
                    // data is a Dictionary<string, string> to add custom data too
                    data.Add("Example string", DateTime.UtcNow.ToString());
                    data.Add("User Id", "You could fetch a user/account Id here, etc.");
                    data.Add("Links get linkified", "http://www.google.com");
                };

            // Setting the jQuery URL, in case you need this to be an internally hosted jQuery for example
            // By default, this will pull from the google CDN
            ErrorStore.jQueryURL = "//ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js";

            ErrorStore.AddJSInclude("~/Content/errors.js");
            ErrorStore.OnBeforeLog += (sender, args) =>
                {
                    args.Error.Message += " - This was appended in the OnBeforeLog handler.";
                    //args.Abort = true; - you could stop the exception from being logged here
                };
            ErrorStore.OnAfterLog += (sender, args) =>
                {
                    Trace.WriteLine("The logged exception GUID was: " + args.ErrorGuid);
                    // optionally var e = args.GetError() to fetch the actual error from the store
                };

            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        /// <summary>
        /// Example method to log an exception to the log...that' not shown to the user.
        /// </summary>
        /// <param name="e">The exception to log</param>
        public static void LogException(Exception e)
        {
            // Note: When dealing with non-web applications, or logging from background threads, 
            // you would pass, null in instead of a HttpContext object.
            ErrorStore.LogException(e, HttpContext.Current);
        }
    }
}