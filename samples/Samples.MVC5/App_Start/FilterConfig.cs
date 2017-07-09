using System;
using System.Web.Mvc;

namespace Samples.MVC5
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            MvcApplication.LogException(new Exception("Startup simulation: RegisterGlobalFilters Exception!"));
        }
    }
}