using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Exceptional;
using System;
using System.Threading.Tasks;

namespace Samples.AspNetCore.Controllers
{
    public class TestController : Controller
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger) => _logger = logger;

        public ActionResult Logger()
        {
            var ex = new Exception("Test Exception for ILogger goodness.");
            _logger.LogError(ex, ex.Message);
            return Content("Check the log!");
        }

        public async Task<ActionResult> Throw()
        {
            await ExceptionalUtils.Test.GetRedisException().LogAsync(ControllerContext.HttpContext).ConfigureAwait(false);
            await new Exception("").LogAsync(ControllerContext.HttpContext).ConfigureAwait(false);

            var ex = new Exception("This is an exception thrown from the Samples project! - Check out the log to see this exception.");
            // here's how your catch/throw might can add more info, for example SQL is special cased and shown in the UI:
            ex.Data["SQL"] = "Select * From FUBAR -- This is a SQL command!";
            ex.Data["Redis-Server"] = "REDIS01";
            ex.Data["Not-Included"] = "This key is skipped, because it's not in the web.config pattern";
            ex.AddLogData("Via Extension", "Some logged data via the .AddLoggedData() method!");
            throw ex;
        }

        public ActionResult ThrowRedis() => throw ExceptionalUtils.Test.GetRedisException();
    }
}
