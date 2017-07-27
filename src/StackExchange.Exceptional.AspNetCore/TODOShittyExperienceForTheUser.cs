using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Web;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// This is a workaround that should never ship.
    /// </summary>
    public static class TODOShittyExperienceForTheUser
    {
        /// <summary>
        /// Method to get custom data for an error; will be called when custom data isn't already present.
        /// </summary>
        public static Action<Exception, HttpContext, Dictionary<string, string>> GetCustomData { get; set; }
    }
}