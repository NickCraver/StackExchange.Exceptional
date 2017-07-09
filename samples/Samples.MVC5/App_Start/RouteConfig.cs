using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace Samples.MVC5
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            // map the /errors route for accessibility via a router, in addition to or instead of a handler directly
            routes.MapRoute(
                name: "Exceptions",
                url: "{controller}/{action}/{resource}/{subResource}",
                defaults:
                    new
                        {
                            controller = "Home",
                            action = "Exceptions",
                            resource = UrlParameter.Optional,
                            subResource = UrlParameter.Optional
                        }
                );
            MvcApplication.LogException(new Exception("Startup simulation: RegisterRoutes Exception!"));
        }
    }
}