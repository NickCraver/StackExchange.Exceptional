using System;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Arguments for the event handler called after an exception is logged
    /// </summary>
    public class ErrorAfterLogEventArgs : EventArgs
    {
        /// <summary>
        /// The Error object in question
        /// </summary>
        public Guid ErrorGuid { get; }

        /// <summary>
        /// Creates an ErrorAfterLogEventArgs object to be passed to event handlers
        /// </summary>
        /// <param name="e">The error to create <see cref="ErrorAfterLogEventArgs"/> for.</param>
        public ErrorAfterLogEventArgs(Error e)
        {
            ErrorGuid = e.GUID;
        }

        ///// <summary>
        ///// Gets the current state of the Error that was logged.
        ///// Important note: since this may be a duplicate of an earlier error it's an explicit fetch from the error store
        ///// </summary>
        ///// <returns>The current state of the Error matching this guid</returns>
        //public Error GetError() => ErrorStore.Default.Get(ErrorGuid);
    }
}
