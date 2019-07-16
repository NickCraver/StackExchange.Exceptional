using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Exceptional;

namespace Samples.AspNetCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Stacks() => View();

        [HttpPost]
        public IActionResult Stacks(IFormCollection form)
        {
            var error = new Exception("").Log(ControllerContext.HttpContext);
            error.Detail = form["details"];
            return Redirect("/Home/Exceptions/info?guid=" + error.GUID);
        }

        /// <summary>
        /// This lets you access the error handler via a route in your application, secured by whatever
        /// mechanisms are already in place.
        /// </summary>
        /// <remarks>If mapping via RouteAttribute: [Route("errors/{path?}/{subPath?}")]</remarks>
        public async Task Exceptions() => await ExceptionalMiddleware.HandleRequestAsync(HttpContext).ConfigureAwait(false);
    }
}
