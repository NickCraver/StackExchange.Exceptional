using StackExchange.Exceptional.Internal;
using System;
using System.Threading.Tasks;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Central settings config point for context-less logging.
    /// </summary>
    public static class Exceptional
    {
        /// <summary>
        /// Settings for context-less logging.
        /// </summary>
        /// <remarks>
        /// In ASP.NET (non-Core) this is populated by the ConfigSettings load.
        /// In ASP.NET Core this is populated by .Configure() in the DI pipeline.
        /// </remarks>
        public static ExceptionalSettingsBase Settings { get; private set; }

        /// <summary>
        /// Returns whether an error passed in right now would be logged.
        /// </summary>
        public static bool IsLoggingEnabled { get; private set; } = true;

        /// <summary>
        /// Re-enables error logging after a <see cref="DisableLogging"/> call.
        /// </summary>
        public static void EnableLogging() => IsLoggingEnabled = true;

        /// <summary>
        /// Disables error logging, call <see cref="EnableLogging"/> to re-enable.
        /// </summary>
        /// <remarks>
        /// This is useful when an <see cref="AppDomain"/> is being torn down, for example <code>IRegisteredObject.Stop()</code> when a web application is being stopped
        /// </remarks>
        public static void DisableLogging() => IsLoggingEnabled = false;

        /// <summary>
        /// Saves the given <paramref name="settings"/> as the global <see cref="Settings"/> available for use globally.
        /// These are intended to be used by global/background handlers where normal context access isn't available.
        /// </summary>
        /// <param name="settings">The settings object to set for background settings.</param>
        public static void Configure(ExceptionalSettingsBase settings) =>
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        /// <summary>
        /// Sets the default error store to use for logging.
        /// </summary>
        /// <param name="store">The error store used to store, e.g. <code>new SQLErrorStore(myConnectionString)</code></param>
        public static void Configure(ErrorStore store) =>
            Settings.DefaultStore = store ?? throw new ArgumentNullException(nameof(store));

        private static readonly EventHandler<UnobservedTaskExceptionEventArgs> taskHandler = (s, args) =>
        {
            foreach (var ex in args.Exception.InnerExceptions)
            {
                ex.LogNoContext(rollupPerServer: true);
            }
            args.SetObserved();
        };

        /// <summary>
        /// Attaches to <see cref="TaskScheduler.UnobservedTaskException"/> and handles all <see cref="Task"/> background exceptions.
        /// Without handling these (or at least observing them) the default behavior is an application pool crashing.
        /// </summary>
        public static void ObserveTaskExceptions()
        {
            // Ensure we don't register twice
            TaskScheduler.UnobservedTaskException -= taskHandler;
            TaskScheduler.UnobservedTaskException += taskHandler;
        }

        private static readonly UnhandledExceptionEventHandler domainHandler = (s, args) =>
        {
            // args.ExceptionObject may not be an exception, refer to http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf
            // section 10.5, CLS Rule 40 if you're curious on why this check needs to happen
            if (args.ExceptionObject is Exception e)
            {
                e.AddLogData("Terminating", args.IsTerminating).LogNoContext();
            }
        };

        /// <summary>
        /// Attaches to <see cref="AppDomain.UnhandledException"/> and handles all global exceptions.
        /// This is useful for console applications where not observing the exception as part of the ASP.NET pipeline.
        /// </summary>
        public static void ObserveAppDomainUnhandledExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException -= domainHandler;
            AppDomain.CurrentDomain.UnhandledException += domainHandler;
        }
    }
}
