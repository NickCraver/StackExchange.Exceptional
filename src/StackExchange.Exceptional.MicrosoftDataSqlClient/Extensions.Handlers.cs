using StackExchange.Exceptional.Internal;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Extensions methods for <see cref="Exception"/>s.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds the default data handlers to a handlers collection.
        /// </summary>
        /// <param name="handlers">The dictionary to register these default handlers on.</param>
        public static Dictionary<string, Action<Error>> AddSqlException(this Dictionary<string, Action<Error>> handlers)
        {
            handlers?.AddHandler<SqlException>((e, se) =>
            {
                e.AddCommand(new Command("SQL Server Query", se.Data.Contains("SQL") ? se.Data["SQL"] as string : null)
                    .AddData(nameof(se.Server), se.Server)
                    .AddData(nameof(se.Number), se.Number.ToString())
                    .AddData(nameof(se.LineNumber), se.LineNumber.ToString())
                    .AddData(se.Procedure.HasValue(), nameof(se.Procedure), se.Procedure)
                );
            });
            return handlers;
        }
    }
}
