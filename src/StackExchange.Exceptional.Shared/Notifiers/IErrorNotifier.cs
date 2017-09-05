namespace StackExchange.Exceptional.Notifiers
{
    /// <summary>
    /// Represents a notifier, something that takes an exception that was just logged and notifies someone or something else.
    /// </summary>
    public interface IErrorNotifier
    {
        /// <summary>
        /// Whether this notifier is enabled.
        /// </summary>
        bool Enabled { get; }
        /// <summary>
        /// Processes an error that was just logged.
        /// </summary>
        /// <param name="error">The error to notify someone or something about.</param>
        void Notify(Error error);
    }
}
