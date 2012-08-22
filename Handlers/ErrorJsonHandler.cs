using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace StackExchange.Exceptional.Handlers
{
    internal sealed class ErrorJsonHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            const int maxCount = 200;
            long sinceLong;
            DateTime since = long.TryParse(context.Request["since"], out sinceLong)
                                 ? new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(sinceLong)
                                 : DateTime.MinValue;

            var errors = new List<Error>(maxCount);
            ErrorStore.Default.GetAll(errors);

            var result = errors.Where(error => error.CreationDate >= since).Select(error => new JsonError(error)).ToList();

            var ser = new JavaScriptSerializer();
            var json = ser.Serialize(result);
            context.Response.Output.Write(json);
        }

        private class JsonError
        {
            public string HostName { get; set; }
            public string Message { get; set; }
            public int DuplicateCount { get; set; }
            public long EpochTime { get; set; }
            public string Id { get; set; }
            public string Type { get; set; }
            public string IP { get; set; }
            public string Host { get; set; }
            public string Url { get; set; }
            public bool Protected { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
            public JsonError(Error error)
            {
                Id = error.Id.ToString();
                Message = error.Message;
                DuplicateCount = error.DuplicateCount ?? 0;
                EpochTime = (long)(error.CreationDate - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                Type = error.Type;
                IP = error.IPAddress;
                Host = error.Host;
                Url = error.Url;
                Protected = error.IsProtected;
                HostName = error.MachineName;
                CustomData = error.CustomData;
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}