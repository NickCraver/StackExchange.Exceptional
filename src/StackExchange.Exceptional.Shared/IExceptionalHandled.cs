using System;

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
        /// Access (arg).Exception to get the instance of the exception thrown here.
        /// The actual action returned (for optimization) should be a single instance, e.g. not a new
        /// Action{T} returned on every get call.
        /// </summary>
        Action<Error> ExceptionalHandler { get; }
    }
}
