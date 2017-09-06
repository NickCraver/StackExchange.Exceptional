namespace StackExchange.Exceptional
{
    /// <summary>
    /// Settings for prettifying a StackTrace
    /// </summary>
    public class StackTraceSettings
    {
        /// <summary>
        /// Replaces generic names like Dictionary`2 with Dictionary&lt;TKey,TValue&gt;.
        /// Specific formatting is based on the <see cref="Language"/> setting.
        /// </summary>
        public bool EnablePrettyGenerics { get; set; } = true;
        /// <summary>
        /// The language to use when prettifying StackTrace generics.
        /// Defaults to C#.
        /// </summary>
        public CodeLanguage Language { get; set; }
        /// <summary>
        /// Whether to print generic type names like &lt;T1, T2&gt; etc. or just use commas, e.g. &lt;,,&gt; if <see cref="Language"/> is C#.
        /// Defaults to true.
        /// </summary>
        public bool IncludeGenericTypeNames { get; set; } = true;

        /// <summary>
        /// The language to use when operating on errors and stack traces.
        /// </summary>
        public enum CodeLanguage
        {
            /// <summary>
            /// C#
            /// </summary>
            CSharp,
            /// <summary>
            /// F#
            /// </summary>
            FSharp,
            /// <summary>
            /// Visual Basic
            /// </summary>
            VB
        }
    }
}