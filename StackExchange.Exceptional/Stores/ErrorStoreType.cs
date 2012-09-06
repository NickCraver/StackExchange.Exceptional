namespace StackExchange.Exceptional.Stores
{
    /// <summary>
    /// Types of ErrorStores available, this may move later as future projects build on Exceptional's core and expand this list
    /// </summary>
    public enum ErrorStoreType
    {
        /// <summary>
        /// A JSON file-based error store
        /// </summary>
        JSON,
        /// <summary>
        /// An in-memory error store
        /// </summary>
        Memory,
        /// <summary>
        /// A SQL based error store
        /// </summary>
        SQL
    }
}
