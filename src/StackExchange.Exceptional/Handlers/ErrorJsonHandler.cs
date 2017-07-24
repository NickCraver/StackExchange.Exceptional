using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;
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

            DateTime since = long.TryParse(context.Request["since"], out long sinceLong)
                     ? new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(sinceLong)
                     : DateTime.MinValue;

            var errors = ErrorStore.Default.GetAll();
            var result = errors.Where(error => error.CreationDate >= since).Select(error => new JsonError(error)).ToList();

            serializer.Serialize(context.Response.Output, result);
        }

        private class JsonError
        {
            public string HostName { get; }
            public string Message { get; }
            public int DuplicateCount { get; }
            public long EpochTime { get; }
            public string Id { get; }
            public string Type { get; }
            public string IP { get; }
            public string Host { get; }
            public string Url { get; }
            public bool Protected { get; }
            public Dictionary<string, string> CustomData { get; }
            public JsonError(Error error)
            {
                Id = error.Id.ToString();
                Message = error.Message;
                DuplicateCount = error.DuplicateCount ?? 0;
                EpochTime = error.CreationDate.ToEpochTime();
                Type = error.Type;
                IP = error.IPAddress;
                Host = error.Host;
                Url = error.Url;
                Protected = error.IsProtected;
                HostName = error.MachineName;
                CustomData = error.CustomData;
            }
        }

        public bool IsReusable => true;
    }
}