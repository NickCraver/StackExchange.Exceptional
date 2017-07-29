using StackExchange.Exceptional.Internal;
using System;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// Extensions methods for <see cref="Exception"/>s.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds a key/value pair for logging to an exception, one that'll appear in exceptional
        /// </summary>
        /// <typeparam name="T">The specific type of exception (for return type chaining).</typeparam>
        /// <param name="ex">The exception itself.</param>
        /// <param name="key">The key to add to the exception data.</param>
        /// <param name="value">The value to add to the exception data.</param>
        public static T AddLogData<T>(this T ex, string key, string value) where T : Exception
        {
            ex.Data[Constants.CustomDataKeyPrefix + key] = value ?? string.Empty;
            return ex;
        }

        /// <summary>
        /// Adds a key/value pair for logging to an exception, one that'll appear in exceptional
        /// </summary>
        /// <typeparam name="T">The specific type of exception (for return type chaining).</typeparam>
        /// <param name="ex">The exception itself.</param>
        /// <param name="key">The key to add to the exception data.</param>
        /// <param name="value">The value to add to the exception data.</param>
        public static T AddLogData<T>(this T ex, string key, object value) where T : Exception => AddLogData(ex, key, value?.ToString());
    }
}
