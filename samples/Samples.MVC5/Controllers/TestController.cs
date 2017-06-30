using StackExchange.Exceptional;
using System;
using System.Transactions;
using System.Web.Mvc;

namespace Samples.MVC5.Controllers
{
    public class TestController : Controller
    {
        public ActionResult Settings()
        {
            return Json(ExceptionalSettings.Current, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Form()
        {
            Response.SetCookie(new System.Web.HttpCookie("authToken", "test value"));
            Response.SetCookie(new System.Web.HttpCookie("notAnAuthToken", "Turnip."));
            ViewBag.Message = "This is a sample with a form which has filtered logging (e.g. password is ommitted).";

            return View();
        }

        public ActionResult FormSubmit(FormCollection fc)
        {
            throw new Exception("Check out the log to see that this exception didn't log the password.");
        }

        public ActionResult TransactionScope()
        {
            using (var t = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions {IsolationLevel = IsolationLevel.ReadCommitted}))
            {
                try
                {
                    throw new Exception("Transaction killing exception of doom!");
                    //t.Complete();
                }
                catch (Exception e)
                {
                    MvcApplication.LogException(e); // this still gets logged, because Exceptional ignores transaction scopes
                }
            }
            return RedirectToAction("Exceptions", "Home");
        }

        public ActionResult Throw()
        {
            var ex = new Exception("This is an exception throw from the Samples project! - Check out the log to see this exception.");
            // here's how your catch/throw might can add more info, for example SQL is special cased and shown in the UI:
            ex.Data["SQL"] = "Select * From FUBAR -- This is a SQL command!";
            ex.Data["Redis-Server"] = "REDIS01";
            ex.Data["Not-Included"] = "This key is skipped, because it's not in the web.config pattern";
            throw ex;
        }
    }
}
