using System;
using System.Runtime.CompilerServices;
using Dapper;
using Npgsql;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class PostgreSqlErrorStoreTest : StoreBaseTest, IClassFixture<PostgreSqlFixture>
    {
        public string ConnectionString => TestConfig.Current.PostgreSqlConnectionString;
        private PostgreSqlFixture Fixture { get; }

        public PostgreSqlErrorStoreTest(PostgreSqlFixture fixture, ITestOutputHelper output) : base(output)
        {
            Fixture = fixture;
            if (Fixture.ShouldSkip)
            {
                Skip.Inconclusive("Couldn't test against to: " + ConnectionString + "\n" + fixture.SkipReason);
            }
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new PostgreSqlErrorStore(new ErrorStoreSettings
            {
                ConnectionString = ConnectionString,
                ApplicationName = appName,
                TableName = Fixture.TableName
            });
    }

    public class PostgreSqlFixture : IDisposable
    {
        public bool ShouldSkip { get; }
        public string SkipReason { get; }
        public string TableName { get; }
        public string TableScript { get; }

        public PostgreSqlFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.PostgreSqlConnectionString), TestConfig.Current.PostgreSqlConnectionString);
            try
            {
                var script = Resource.Get("Scripts.PostgreSql.sql");
                using (var conn = new NpgsqlConnection(TestConfig.Current.PostgreSqlConnectionString))
                {
                    TableName = $@"public.""Test{Guid.NewGuid().ToString("N").Substring(24)}""";
                    TableScript = script.Replace(@"""public"".""Errors""", TableName);
                    conn.Execute(TableScript);
                }
            }
            catch (Exception e)
            {
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        public void Dispose()
        {
            try
            {
                using (var conn = new NpgsqlConnection(TestConfig.Current.PostgreSqlConnectionString))
                {
                    conn.Execute("Drop Table " + TableName);
                }
            }
            catch when (ShouldSkip)
            {
                // if we didn't error initially then we'll throw
            }
        }
    }
}
