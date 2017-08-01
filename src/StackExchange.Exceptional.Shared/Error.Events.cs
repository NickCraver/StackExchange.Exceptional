using System;

namespace StackExchange.Exceptional
{
    public partial class Error
    {
        /// <summary>
        /// Event handler to run before an exception is logged to the store.
        /// </summary>
        public static event EventHandler<ErrorBeforeLogEventArgs> OnBeforeLog;

        /// <summary>
        /// Event handler to run after an exception has been logged to the store.
        /// </summary>
        public static event EventHandler<ErrorAfterLogEventArgs> OnAfterLog;

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

        /// <summary>
        /// Arguments for the event handler called after an exception is logged.
        /// </summary>
        public class ErrorAfterLogEventArgs : EventArgs
        {
            /// <summary>
            /// The Error object in question.
            /// </summary>
            public Error Error { get; }

            /// <summary>
            /// Creates an ErrorAfterLogEventArgs object to be passed to event handlers.
            /// </summary>
            /// <param name="e">The error to create <see cref="ErrorAfterLogEventArgs"/> for.</param>
            public ErrorAfterLogEventArgs(Error e) => Error = e;
        }
    }
}
