using System;

namespace StackExchange.Exceptional
{
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
