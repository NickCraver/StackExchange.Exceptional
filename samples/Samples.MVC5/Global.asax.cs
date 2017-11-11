using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using StackExchange.Exceptional;

namespace Samples.MVC5
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Instead of any web.config entries, you can perform setup entirely through code
            // Setup Exceptional:

            // Memory example:
            //Exceptional.Configure(settings => settings.DefaultStore = new MemoryErrorStore());
            // JSON example
            //Exceptional.Configure(settings => settings.DefaultStore = new JSONErrorStore(path: "~/Errors"));
            // SQL Example
            //Exceptional.Configure(settings => settings.DefaultStore = new SQLErrorStore(applicationName: "My Error Log Name", connectionString: "Data Source=.;Initial Catalog=Exceptions;Integrated Security=SSPI;"));

            Exceptional.Configure(settings =>
            {
                // Optionally add custom data to any logged exception (visible on the exception detail page):
                settings.GetCustomData = (exception, data) =>
                {
                    // exception is the exception thrown
                    // context is the HttpContext of the request (could be null, e.g. background thread exception)
                    // data is a Dictionary<string, string> to add custom data too
                    data.Add("Example string", DateTime.UtcNow.ToString());
                    data.Add("User Id", "You could fetch a user/account Id here, etc.");
                    data.Add("Links get linkified", "https://www.google.com");
                };
                // Example of how to log command data for anything you want
                // These display the command and the data key/value pairs in the log
                settings.ExceptionActions.AddHandler<ExceptionalUtils.Test.RedisException>((e, ex) =>
                {
                    var cmd = e.AddCommand(new Command("Redis"));
                    foreach (string k in ex.Data.Keys)
                    {
                        var val = ex.Data[k] as string;
                        if (k == "redis-command") cmd.CommandString = val;
                        if (k.StartsWith("Redis-")) cmd.AddData(k.Substring("Redis-".Length), val);
                    }
                });

                settings.Render.JSIncludes.Add("/Content/errors.js");
                settings.OnBeforeLog += (sender, args) =>
                {
                    args.Error.Message += " (suffix from OnBeforeLog handler)";
                    //args.Abort = true; - you could stop the exception from being logged here
                };
                settings.OnAfterLog += (sender, args) =>
                {
                    Trace.WriteLine("The logged exception GUID was: " + args.Error.GUID.ToString());
                    // optionally var e = args.GetError() to fetch the actual error from the store
                };
            });

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
            e.Log(HttpContext.Current);
        }
    }
}
