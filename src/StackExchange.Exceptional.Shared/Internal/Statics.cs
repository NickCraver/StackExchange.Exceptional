using System;
using System.Collections.Generic;

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
        public static ExceptionalSettingsBase Settings { get; set; }

        /// <summary>
        /// Returns whether an error passed in right now would be logged.
        /// </summary>
        public static bool IsLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Default exception actions to handle for any exception types globally.
        /// </summary>
        public static Dictionary<string, Action<Error>> DefaultExceptionActions { get; }

        static Statics()
        {
            DefaultExceptionActions = new Dictionary<string, Action<Error>>();
            DefaultExceptionActions.AddHandler("StackRedis.CacheException",
                (e, ex) =>
                {
                    var cmd = e.AddCommand(new Command("Redis"));
                    foreach (string k in ex.Data.Keys)
                    {
                        var val = ex.Data[k] as string;
                        if (k == "redis-command") cmd.CommandString = val;
                        if (k.StartsWith("Redis-")) cmd.AddData(k.Substring("Redis-".Length), val);
                    }
                });
            Settings = new ExceptionalSettingsDefault();
        }
    }
}
