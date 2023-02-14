using System;

namespace StackExchange.Exceptional.Sqlite
{
    internal class ErrorMapSqlite
    {
        public string ApplicationName { get; set; }
        public string Category { get; set; }
        public DateTime CreationDate { get; set; }
        public string Detail { get; set; }
        public DateTime? DeletionDate { get; set; }
        public int? DuplicateCount { get; set; }
        public int? ErrorHash { get; set; }
        public string FullJson { get; set; }
        public string GUID { get; set; }
        public string FullUrl { get; set; }
        public string HTTPMethod { get; set; }
        public string Host { get; set; }
        public long Id { get; set; }
        public bool IsDuplicate { get; set; }
        public string IPAddress { get; set; }
        public bool IsProtected { get; set; }
        public string Source { get; set; }
        public DateTime? LastLogDate { get; set; }
        public string MachineName { get; set; }
        public string Message { get; set; }
        public int? StatusCode { get; set; }
        public string Type { get; set; }
        public string UrlPath { get; set; }
    }
}
