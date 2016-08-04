using System.Web;
using System.Web.Http.Filters;

namespace StackExchange.Exceptional.WebApi
{
	public class WebAPIExceptionHandlerAttribute : ExceptionFilterAttribute, IExceptionFilter
	{
		public override void OnException(HttpActionExecutedContext actionExecutedContext)
		{
			if (HttpContext.Current != null)
			{
				ErrorStore.LogException(actionExecutedContext.Exception, HttpContext.Current);
			}
			else
			{
				ErrorStore.LogExceptionWithoutContext(actionExecutedContext.Exception);
			}
			base.OnException(actionExecutedContext);
		}
	}
}
