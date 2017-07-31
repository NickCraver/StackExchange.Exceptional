using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace StackExchange.Exceptional
{
    public static partial class Extensions
    {
        /// <summary>
        /// Adds the default data handlers to a handlers collection.
        /// </summary>
        /// <param name="handlers">The dictionary to register these default handlers on.</param>
        public static Dictionary<string, Action<Error>> AddDefault(this Dictionary<string, Action<Error>> handlers)
        {
            handlers?.AddHandler<SqlException>((e, se) =>
            {
                if (se.Data == null) return;
                e.AddCommand(new Command("SQL Server Query", se.Data.Contains("SQL") ? se.Data["SQL"] as string : null)
                    .AddData(nameof(se.Server), se.Server)
                    .AddData(nameof(se.Number), se.Number.ToString())
                    .AddData(nameof(se.LineNumber), se.LineNumber.ToString())
                    .AddData(se.Procedure.HasValue(), nameof(se.Procedure), se.Procedure)
                );
            });
            handlers?.AddHandler("StackRedis.CacheException", (e, ex) =>
            {
                var cmd = e.AddCommand(new Command("Redis"));
                foreach (string k in ex.Data.Keys)
                {
                    var val = ex.Data[k] as string;
                    if (k == "redis-command") cmd.CommandString = val;
                    if (k.StartsWith("Redis-")) cmd.AddData(k.Substring("Redis-".Length), val);
                }
            });

            return handlers;
        }

        /// <summary>
        /// Convenience method for adding a handler for an exception type.
        /// </summary>
        /// <typeparam name="T">The specific type of <see cref="Exception"/> to handle.</typeparam>
        /// <param name="handlers">The handlers collection to add to (usually Settings.Current.DataHandlers)</param>
        /// <param name="handler">The handler action to use.</param>
        public static void AddHandler<T>(this Dictionary<string, Action<Error>> handlers, Action<Error, T> handler) where T : Exception
        {
            handlers[typeof(T).FullName] = e =>
            {
                if (e.Exception is T tex)
                {
                    handler(e, tex);
                }
            };
        }

        /// <summary>
        /// Convenience method for adding a handler for an exception type. Note that the handler here doesn't have the exception
        /// (due to not having a reference to every exception on earth), so behavior is limited to Exception for things like .Data, etc.
        /// </summary>
        /// <param name="handlers">The handlers collection to add to (usually Settings.Current.DataHandlers)</param>
        /// <param name="typeName">The full name of the type, e.g. "System.Data.SqlClient.SqlException"</param>
        /// <param name="handler">The handler action to use.</param>
        public static void AddHandler(this Dictionary<string, Action<Error>> handlers, string typeName, Action<Error, Exception> handler)
        {
            handlers[typeName] = e => handler(e, e.Exception);
        }
    }
}
