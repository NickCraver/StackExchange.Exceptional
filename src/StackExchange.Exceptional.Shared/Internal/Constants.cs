namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Storage for constants used for libraries and views.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Key for storing errors that happen when fetching custom data.
        /// </summary>
        public const string CustomDataErrorKey = "CustomDataFetchError";

        /// <summary>
        /// Key for storing errors that happen when fetching data from a collection in the request, e.g. ServerVariables, Cookies, etc.
        /// </summary>
        public const string CollectionErrorKey = "CollectionFetchError";

        /// <summary>
        /// Key for prefixing fields in .Data for logging to CustomData
        /// </summary>
        public const string CustomDataKeyPrefix = "ExceptionalCustom-";
    }
}
