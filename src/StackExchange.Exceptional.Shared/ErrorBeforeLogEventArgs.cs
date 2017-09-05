using System;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Arguments for the event handler called before an exception is logged.
    /// </summary>
    public class ErrorBeforeLogEventArgs : EventArgs
    {
        /// <summary>
        /// Whether to abort the logging of this exception, if set to true the exception will not be logged.
        /// </summary>
        public bool Abort { get; set; }

        /// <summary>
        /// The Error object in question.
        /// </summary>
        public Error Error { get; }

        /// <summary>
        /// Creates an ErrorBeforeLogEventArgs object to be passed to event handlers, setting .Abort = true prevents the error from being logged.
        /// </summary>
        /// <param name="e">The error to create <see cref="ErrorBeforeLogEventArgs"/> for.</param>
        public ErrorBeforeLogEventArgs(Error e) => Error = e;
    }
}
