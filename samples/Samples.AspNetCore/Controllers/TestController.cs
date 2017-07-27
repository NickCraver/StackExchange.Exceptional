﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Exceptional;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Samples.MVC5.Controllers
{
    public partial class TestController : Controller
    {
        public ActionResult Settings()
        {
            return Json(StackExchange.Exceptional.Settings.Current);
        }

        public ActionResult Form()
        {
            Response.Cookies.Append("authToken", "test value");
            Response.Cookies.Append("notAnAuthToken", "Turnip.");
            ViewBag.Message = "This is a sample with a form which has filtered logging (e.g. password is omitted).";

            return View();
        }

        public ActionResult FormSubmit(FormCollection fc)
        {
            var used = fc;
            throw new Exception("Check out the log to see that this exception didn't log the password.");
        }


        public ActionResult Throw()
        {
            var ex = new Exception("This is an exception thrown from the Samples project! - Check out the log to see this exception.");
            // here's how your catch/throw might can add more info, for example SQL is special cased and shown in the UI:
            ex.Data["SQL"] = "Select * From FUBAR -- This is a SQL command!";
            ex.Data["Redis-Server"] = "REDIS01";
            ex.Data["Not-Included"] = "This key is skipped, because it's not in the web.config pattern";
            throw ex;
        }

#pragma warning disable RCS1174 // Remove redundant async/await.
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
        public async Task<ActionResult> ThrowAsync() => await RelayC().ConfigureAwait(false);

        public async Task<ActionResult> RelayA() => await RelayC().ConfigureAwait(false);

        public async Task<ActionResult> RelayB() => await RelayC().ConfigureAwait(false);
#pragma warning restore RCS1174 // Remove redundant async/await.
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.

        public async Task<ActionResult> RelayC()
        {
            try
            {
                await Utils.Test.ThrowStackAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                TestLog(e);
            }
            // TODO: Make these switchable in the UI somewhere
            //ExceptionalSettings.Current.StackTrace.IncludeGenericTypeNames = false;
            //ExceptionalSettings.Current.StackTrace.Language = ExceptionalSettings.StackTraceSettings.CodeLanguage.FSharp;
            (new MyGenericClass<string, bool, int, long, string, bool?, int?, long?>()).Throw<string, int>();
            return Content("Not here!"); // this will throw a KeyNotFoundException
        }

        private class MyGenericClass<T1, T2, T3, T4, T5, T6, T7, T8>
        {
            public string Throw<TThrow, TThrow2>()
            {
                var dict = new Dictionary<string, string>();
                return dict["Not here!" + typeof(TThrow).Name + typeof(TThrow2).Name];
            }
        }

        public ActionResult ThrowStacks()
        {
            TestLog(
                new ExplicitStackException("Test Dictionary", "KeyNotFoundException The given key was not present in the dictionary. at System.Collections.Generic.Dictionary`2.get_Item(TKey key) at Hangfire.SqlServer.SqlServerMonitoringApi.<>c.<DeletedJobs>b__15_1(SqlJob sqlJob, Job job, Dictionary`2 stateData) at Hangfire.SqlServer.SqlServerMonitoringApi.DeserializeJobs[TDto](ICollection`1 jobs, Func`4 selector) at Hangfire.SqlServer.SqlServerMonitoringApi.GetJobs[TDto](DbConnection connection, Int32 from, Int32 count, String stateName, Func`4 selector) at Hangfire.SqlServer.SqlServerMonitoringApi.<>c__DisplayClass15_0.<DeletedJobs>b__0(DbConnection connection) at Hangfire.SqlServer.SqlServerStorage.UseConnection[T](Func`2 func) at Hangfire.SqlServer.SqlServerMonitoringApi.DeletedJobs(Int32 from, Int32 count) at Hangfire.Dashboard.Pages.DeletedJobsPage.Execute() at Hangfire.Dashboard.RazorPage.TransformText(String body) at Hangfire.Dashboard.RazorPageDispatcher.Dispatch(DashboardContext context) at Hangfire.Dashboard.MiddlewareExtensions.<>c__DisplayClass1_2.<UseHangfireDashboard>b__1(IDictionary`2 env) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Mapping.MapMiddleware.<Invoke>d__0.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Host.SystemWeb.IntegratedPipeline.IntegratedPipelineContextStage.<RunApp>d__5.MoveNext() --- End of stack trace from previous location where exception was thrown --- at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) at Microsoft.Owin.Host.SystemWeb.IntegratedPipeline.IntegratedPipelineContext.<DoFinalWork>d__2.MoveNext() --- End of stack trace from previous location where exception was thrown --- at Microsoft.Owin.Host.SystemWeb.IntegratedPipeline.StageAsyncResult.End(IAsyncResult ar) at System.Web.HttpApplication.AsyncEventExecutionStep.System.Web.HttpApplication.IExecutionStep.Execute() at System.Web.HttpApplication.ExecuteStep(IExecutionStep step, Boolean& completedSynchronously)")
            );
            return Content("Thrown!");
        }

        private void TestLog(params Exception[] errors)
        {
            foreach (var e in errors)
            {
                e.Log(HttpContext);
            }
        }
#pragma warning restore RCS1194 // Implement exception constructors.
    }
}
