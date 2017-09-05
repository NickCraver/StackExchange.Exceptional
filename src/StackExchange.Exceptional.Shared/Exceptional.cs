using System;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Central settings config point for context-less logging.
    /// </summary>
    public class Exceptional
    {
        /// <summary>
        /// Settings for context-less logging.
        /// </summary>
        /// <remarks>
        /// In ASP.NET (non-Core) this is populated by the ConfigSettings load.
        /// In ASP.NET Core this is populated by .Configure() in the DI pipeline.
        /// </remarks>
        public static SettingsBase Settings { get; private set; }

        /// <summary>
        /// Initializes this settings store.
        /// </summary>
        /// <param name="settings"></param>
        public static void Configure(SettingsBase settings) => 
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        /// <summary>
        /// Sets the default error store to use for logging
        /// </summary>
        /// <param name="store">The error store used to store, e.g. <code>new SQLErrorStore(myConnectionString)</code></param>
        public static void Configure(ErrorStore store) => 
            Settings.DefaultStore = store ?? throw new ArgumentNullException(nameof(store));
    }
}
