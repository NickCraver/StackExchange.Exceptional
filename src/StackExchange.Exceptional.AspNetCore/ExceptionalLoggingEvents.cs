namespace StackExchange.Exceptional
{
    /// <summary>
    /// Events IDs for known exceptional events while logging.
    /// </summary>
    public static class ExceptionalLoggingEvents
    {
        internal const int Min = RequestException;
        internal const int Max = ExceptionalPageException;

        /// <summary>
        /// A request threw an exception, caught by Exceptional Middleware.
        /// </summary>
        public const int RequestException = 77000;

        /// <summary>
        /// A request was thrown while trying to render the Exceptional error page.
        /// </summary>
        public const int ExceptionalPageException = 77001;
    }
}
