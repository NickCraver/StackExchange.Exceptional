using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        /// Link replacements to run on the stack trace, e.g. for linkifying SourceLink to GitHub, etc.
        /// </summary>
        public Dictionary<Regex, string> LinkReplacements { get; } = new Dictionary<Regex, string>();

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

        /// <summary>
        /// Adds a <see cref="Regex"/>-based replacement to <see cref="LinkReplacements"/>.
        /// </summary>
        /// <param name="matchPattern">The pattern for the <see cref="Regex"/>.</param>
        /// <param name="repalcementPattern">The replacement pattern.</param>
        public void AddReplacement(string matchPattern, string repalcementPattern)
        {
            LinkReplacements[new Regex(matchPattern, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant)] = repalcementPattern;
        }

        /// <summary>
        /// Creates a new instance of <see cref="StackTraceSettings"/> with default replacements.
        /// </summary>
        public StackTraceSettings()
        {
            // TODO: Other major SourceLink providers
            AddReplacement("https?://raw\\.githubusercontent\\.com/([^/]+/)([^/]+/)([^/]+/)(.*?):line (\\d+)", "<a href=\"https://github.com/$1$2blob/$3$4#L$5\">$4:line $5</a>");
        }
    }
}
