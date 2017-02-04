using System;
using System.Collections.Generic;
using System.Web;
using StackExchange.Exceptional.Extensions;
using System.Collections.Specialized;

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
        /// The URL to use for jQuery on the pages rendered by Exceptional
        /// </summary>
        public static string jQueryURL = "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js";

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

        /// <summary>
        /// Method of getting the IP address for the error, defaults to retrieving it from server variables
        /// but may need to be replaced in special nulti-proxy situations.
        /// </summary>
        public static Func<string> GetIPAddress { get; set; }

        /// <summary>
        /// Event handler to run before an exception is logged to the store
        /// </summary>
        public static event EventHandler<ErrorBeforeLogEventArgs> OnBeforeLog;

        /// <summary>
        /// Event handler to run after an exception has been logged to the store
        /// </summary>
        public static event EventHandler<ErrorAfterLogEventArgs> OnAfterLog;
    }

    /// <summary>
    /// Arguments for the event handler called before an exception is logged
    /// </summary>
    public class ErrorBeforeLogEventArgs : EventArgs
    {
        /// <summary>
        /// Whether to abort the logging of this exception, if set to true the exception will not be logged.
        /// </summary>
        public bool Abort { get; set; }
        /// <summary>
        /// The Error object in question
        /// </summary>
        public Error Error { get; private set; }

        /// <summary>
        /// Creates an ErrorBeforeLogEventArgs object to be passed to event handlers, setting .Abort = true prevents the error from being logged.
        /// </summary>
        public ErrorBeforeLogEventArgs(Error e)
        {
            Error = e;
        }
    }

    /// <summary>
    /// Arguments for the event handler called after an exception is logged
    /// </summary>
    public class ErrorAfterLogEventArgs : EventArgs
    {
        /// <summary>
        /// The Error object in question
        /// </summary>
        public Guid ErrorGuid { get; private set; }
        /// <summary>
        /// Creates an ErrorAfterLogEventArgs object to be passed to event handlers
        /// </summary>
        public ErrorAfterLogEventArgs(Error e)
        {
            ErrorGuid = e.GUID;
        }

        /// <summary>
        /// Gets the current state of the Error that was logged.
        /// Important note: since this may be a duplicate of an earlier error it's an explicit fetch from the error store
        /// </summary>
        /// <returns>The current state of the Error matching this guid</returns>
        public Error GetError()
        {
            return ErrorStore.Default.Get(ErrorGuid);
        }
    }
}
