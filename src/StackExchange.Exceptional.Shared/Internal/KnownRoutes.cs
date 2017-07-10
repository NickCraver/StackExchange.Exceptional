namespace StackExchange.Exceptional.Internal
{
    /// <summary>
    /// Internal Exceptional collection, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// </summary>
    public static class KnownRoutes
    {
        /// <summary>/delete</summary>
        public const string Delete = "delete";
        /// <summary>/delete-all</summary>
        public const string DeleteAll = "delete-all";
        /// <summary>/delete-list</summary>
        public const string DeleteList = "delete-list";
        /// <summary>/protect</summary>
        public const string Protect = "protect";
        /// <summary>/protect-list</summary>
        public const string ProtectList = "protect-list";

        /// <summary>/info</summary>
        public const string Info = "info";
        /// <summary>/json</summary>
        public const string Json = "json";
        /// <summary>/css</summary>
        public const string Css = "css";
        /// <summary>/js</summary>
        public const string Js = "js";
        /// <summary>/test</summary>
        public const string Test = "test";
    }
}
