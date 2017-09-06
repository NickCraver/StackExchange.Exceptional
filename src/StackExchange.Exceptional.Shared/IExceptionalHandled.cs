namespace StackExchange.Exceptional
{
    /// <summary>
    /// Interface an Exception type can implement that handles adding log data to exceptional.
    /// This allows all the relevant data to be logged in the Exception's class rather that in
    /// the logging config elsewhere.
    /// </summary>
    public interface IExceptionalHandled
    {
        /// <summary>
        /// The handler or "what to do" when Exceptional logs an exception of this type
        /// Access <paramref name="e"/>.Exception to get the instance of the exception thrown here.
        /// </summary>
        /// <param name="e">The error wrapper of the exception to handle.</param>
        void ExceptionalHandler(Error e);
    }
}
