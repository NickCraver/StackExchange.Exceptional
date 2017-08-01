using System.Collections.Generic;

namespace StackExchange.Exceptional
{
    /// <summary>
    /// A command to log along with an error.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// The type of this command, e.g. "SQL Server Query" 
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The string that goes with this command, e.g. the SQL query itself.
        /// </summary>
        public string CommandString { get; set; }
        /// <summary>
        /// Data attributes about the command, e.g. the SQL, Redis, or Elastic Server, the timeout...whatever may help 
        /// debug an error can be logged here.
        /// </summary>
        public Dictionary<string, string> Data { get; set; }

        /// <summary>
        /// Creates a new <see cref="Command"/> with the given <see cref="Type"/> and (optionally) <see cref="CommandString"/>.
        /// Commands without a command string may still be useful for when other data is present.
        /// </summary>
        /// <param name="type">The type of this command, e.g. "SQL Server Query".</param>
        /// <param name="commandString">The string that goes with this command, e.g. the SQL query itself.</param>
        public Command(string type, string commandString = null)
        {
            Type = type;
            CommandString = commandString;
        }

        /// <summary>
        /// Adds data for this command, for key/value display later.
        /// </summary>
        /// <param name="key">The key for this data.</param>
        /// <param name="value">The value for this data.</param>
        public Command AddData(string key, string value)
        {
            (Data ?? (Data = new Dictionary<string, string>())).Add(key, value);
            return this;
        }

        /// <summary>
        /// Adds data for this command, for key/value display later.
        /// </summary>
        /// <param name="addIf">Whether to add this data.</param>
        /// <param name="key">The key for this data.</param>
        /// <param name="value">The value for this data.</param>
        public Command AddData(bool addIf, string key, string value) => addIf ? AddData(key, value) : this;

        /// <summary>
        /// Gets the inferred language for Highlight.js, e.g. "sql" for SQL.
        /// </summary>
        /// <returns>The specific highlight.js language to use, or empty if unknown or inferred well already.</returns>
        public string GetHighlightLanguage()
        {
            // URLs
            if (CommandString?.StartsWith("http://") == true || CommandString?.StartsWith("https://") == true) return "html";

            // Languages that are inferred well:
            //if (Type.Contains("SQL")) return "sql";
            //if (CommandString?.StartsWith("{") == true && CommandString?.EndsWith("}") == true) return "json";
            //if (CommandString?.StartsWith("[") == true && CommandString?.EndsWith("]") == true) return "json";

            return string.Empty;
        }
    }
}
