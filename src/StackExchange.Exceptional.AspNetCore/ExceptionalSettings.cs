using Microsoft.AspNetCore.Http;
using System;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// ASP.NET Core settings for Exceptional error logging.
    /// </summary>
    public class ExceptionalSettings : SettingsBase
    {
        /// <summary>
        /// Whether to show the Exceptional page on throw, instead of the built-in .UseDeveloperExceptionPage()
        /// </summary>
        public bool UseExceptionalPageOnThrow { get; set; }

        /// <summary>
        /// Method of getting the IP address for the error, defaults to retrieving it from server variables.
        /// but may need to be replaced in special multi-proxy situations.
        /// </summary>
        public Func<HttpContext, string> GetIPAddress { get; set; } = context => context.Connection.RemoteIpAddress.ToString();
    }
}
