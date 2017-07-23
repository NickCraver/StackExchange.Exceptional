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

    /// <summary>
    /// Extensions for <see cref="IErrorNotifier"/>.
    /// </summary>
    public static class ErrorNotifierExtensions
    {
        /// <summary>
        /// Registers an <see cref="IErrorNotifier"/>, returning the notifier for chaining purposes.
        /// If the notifier is already registered, it is not registered again.
        /// </summary>
        /// <typeparam name="T">The notifier type to register (this should be inferred generics).</typeparam>
        /// <param name="notifier">The notifier to register.</param>
        public static T Register<T>(this T notifier) where T : IErrorNotifier
        {
            var notifiers = Settings.Current.Notifiers;
            if (!notifiers.Contains(notifier))
            {
                notifiers.Add(notifier);
            }
            return notifier;
        }

        /// <summary>
        /// De-registers a notifier, for disabling a notifier at runtime.
        /// </summary>
        /// <param name="notifier">The notifier to remove.</param>
        /// <returns>Whether the notifier was removed. <c>false</c> indicates it was not present.</returns>
        public static bool Deregister(IErrorNotifier notifier) => Settings.Current.Notifiers.Remove(notifier);
    }
}
