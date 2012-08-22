using System;
using System.Collections.Generic;
using System.Web;
using StackExchange.Exceptional.Extensions;

namespace StackExchange.Exceptional
{
    public abstract partial class ErrorStore
    {
        internal static List<string> JSIncludes = new List<string>();
        internal static List<string> CSSIncludes = new List<string>();

        /// <summary>
        /// Adds a JavaScript include to all error log pages, for customizing the behavior and such
        /// </summary>
        /// <param name="path">The path of the JS file, app-relative ~/ are allowed</param>
        public static void AddJSInclude(string path)
        {
            JSIncludes.Add(path.ResolveRelativeUrl());
        }

        /// <summary>
        /// Adds a CSS include to all error log pages, for customizing the look and feel
        /// </summary>
        /// <param name="path">The path of the CSS file, app-relative ~/ are allowed</param>
        public static void AddCSSInclude(string path)
        {
            CSSIncludes.Add(path.ResolveRelativeUrl());
        }

        /// <summary>
        /// Re-enables error logging afer a .DisableLogging() call
        /// </summary>
        public static void EnableLogging()
        {
            _enableLogging = true;
        }

        /// <summary>
        /// Disables error logging, call .EnableLogging() to re-enable
        /// </summary>
        /// <remarks>
        /// This is useful when an app domain is being torn down, for example <code>IRegisteredObject.Stop()</code> when a web application is being stopped
        /// </remarks>
        public static void DisableLogging()
        {
            _enableLogging = false;
        }

        /// <summary>
        /// Returns whether an error passed in right now would be logged
        /// </summary>
        public static bool IsLoggingEnabled { get { return _enableLogging; } }

        /// <summary>
        /// Method to get custom data for an error for, will be call when custom data isn't already present
        /// </summary>
        public static Action<Exception, HttpContext, Dictionary<string, string>> GetCustomData { get; set; } 
    }
}
