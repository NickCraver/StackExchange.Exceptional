using System;
using System.Web.Optimization;

namespace Samples.MVC5
{
    public static class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/site.css"));
            MvcApplication.LogException(new Exception("Startup simulation: RegisterBundles Exception!"));
        }
    }
}