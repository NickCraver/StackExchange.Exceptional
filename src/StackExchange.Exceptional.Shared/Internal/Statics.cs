namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Internal Exceptional static controls, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// </summary>
    public static class Statics
    {
        /// <summary>
        /// Settings for context-less logging.
        /// </summary>
        /// <remarks>
        /// In ASP.NET (non-Core) this is populated by the ConfigSettings load.
        /// In ASP.NET Core this is populated by .Configure() in the DI pipeline.
        /// </remarks>
        public static ExceptionalSettingsBase Settings { get; set; } = new ExceptionalSettingsDefault();

        /// <summary>
        /// Returns whether an error passed in right now would be logged.
        /// </summary>
        public static bool IsLoggingEnabled { get; set; } = true;
    }
}
