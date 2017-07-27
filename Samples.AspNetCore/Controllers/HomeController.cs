using StackExchange.Exceptional;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Samples.MVC5.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string validationTest)
        {
            ViewBag.Message = "This is a sample showing how to integrate Exceptional into your MVC4 application.";

            // For testing RequestValidationException, test something like: ?validationTest=<!2342!@#$!@#R<!#2, to throw an exception.
            var val = Request.Query[validationTest];

            return View();
        }

        /// <summary>
        /// This lets you access the error handler via a route in your application, secured by whatever
        /// mechanisms are already in place.
        /// </summary>
        /// <remarks>If mapping via RouteAttribute: [Route("errors/{path?}/{subPath?}")]</remarks>
        public async Task<IActionResult> Exceptions()
        { 
            RequestDelegate next = (i) => { return null; };
            await new HandlerFactoryMiddleware(next).Invoke(HttpContext);      
            return null;
        }
    }
}
