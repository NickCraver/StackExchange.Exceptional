using Newtonsoft.Json;
using StackExchange.Exceptional.Internal;
using System;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// ASP.NET Core settings for Exceptional error logging.
    /// </summary>
    public class ExceptionalSettings : ExceptionalSettingsBase
    {
        /// <summary>
        /// Method of getting the IP address for the error, defaults to retrieving it from server variables.
        /// but may need to be replaced in special multi-proxy situations.
        /// </summary>
        [JsonIgnore]
        public Func<HttpContext, string> GetIPAddress { get; set; } = context => context.Request.ServerVariables?.GetRemoteIP();
    }
}
