﻿using Jil;
using System;

namespace StackExchange.Exceptional.Tests
{
    public static class TestConfig
    {
        private const string FileName = "TestConfig.json";

        public static Config Current { get; }

        static TestConfig()
        {
            Current = new Config();
            try
            {
                var json = Resource.Get(FileName);
                if (!string.IsNullOrEmpty(json))
                {
                    Current = JSON.Deserialize<Config>(json);
                    Console.WriteLine("  {0} found, using for configuration.", FileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Deserializing TestConfig.json: " + ex);
            }
        }

        public class Config
        {
            public bool RunLongRunning { get; set; }
            public bool EnableTestLogging { get; set; } = Environment.GetEnvironmentVariable(nameof(EnableTestLogging)) == "true";

            public string SQLServerConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(SQLServerConnectionString)) ?? "Server=.;Database=tempdb;Trusted_Connection=True;";
            public string MySQLConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(MySQLConnectionString)) ?? "server=localhost;uid=root;pwd=root;database=test;Allow User Variables=true";
            public string PostgreSqlConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(PostgreSqlConnectionString)) ?? "Server=localhost;Port=5432;Database=test;User Id=postgres;Password=postgres;";
            public string MongoDBConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(MongoDBConnectionString)) ?? "mongodb://localhost/test";
            public string OracleConnectionString { get; set; } = Environment.GetEnvironmentVariable(nameof(OracleConnectionString)) ?? "User Id=root;Password=root;Data Source=127.0.0.0:1521";
        }
    }
}
