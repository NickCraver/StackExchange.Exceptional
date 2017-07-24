using Newtonsoft.Json;
using System;
using System.Linq;
using System.Web;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class ErrorJsonHandler : IHttpHandler
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            DateTime? since = long.TryParse(context.Request["since"], out long sinceLong)
                     ? new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(sinceLong)
                     : (DateTime?)null;

            var errors = ErrorStore.Default.GetAll();
            if (since.HasValue)
            {
                errors = errors.Where(error => error.CreationDate >= since).ToList();
            }
            serializer.Serialize(context.Response.Output, errors);
        }

        public bool IsReusable => true;
    }
}