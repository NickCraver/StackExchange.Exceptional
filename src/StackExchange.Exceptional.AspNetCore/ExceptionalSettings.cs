using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Exceptional.Internal;
using System;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// ASP.NET Core settings for Exceptional error logging.
    /// </summary>
    public class ExceptionalSettings : ExceptionalSettingsBase
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

        /// <summary>
        /// The minimum log level for <see cref="ExceptionalLogger"/>.
        /// Defaults to <see cref="Microsoft.Extensions.Logging.LogLevel.Error"/>.
        /// </summary>
        public LogLevel ILoggerLevel { get; set; } = LogLevel.Error;
    }
}
