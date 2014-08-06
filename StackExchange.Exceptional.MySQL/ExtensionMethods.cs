using System;

namespace StackExchange.Exceptional.MySQL
{
    internal static class ExtensionMethods
    {

        /// <summary>
        /// Answers true if this String is either null or empty.
        /// </summary>
        /// <remarks>I'm so tired of typing String.IsNullOrEmpty(s)</remarks>
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        /// <summary>
        ///     Answers true if this String is neither null or empty.
        /// </summary>
        /// <remarks>I'm also tired of typing !String.IsNullOrEmpty(s)</remarks>
        public static bool HasValue(this string s)
        {
            return !String.IsNullOrEmpty(s);
        }

        /// <summary>
        ///     Returns the first non-null/non-empty parameter when this String is null/empty.
        /// </summary>
        public static string IsNullOrEmptyReturn(this string s, params string[] otherPossibleResults)
        {
            if (s.HasValue())
                return s;

            foreach (string t in otherPossibleResults ?? new string[0])
            {
                if (t.HasValue())
                    return t;
            }

            return "";
        }

        /// <summary>
        ///     force string to be maxlen or smaller
        /// </summary>
        public static string Truncate(this string s, int maxLength)
        {
            return (s.HasValue() && s.Length > maxLength) ? s.Remove(maxLength) : s;
        }
    }
}