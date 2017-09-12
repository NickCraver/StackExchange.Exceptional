using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using Dapper;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class SQLStorage : StoreBase, IClassFixture<SqlFixture>
    {
        public string ConnectionString => TestConfig.Current.SQLConnectionString;
        private SqlFixture Fixtue { get; }

        public SQLStorage(SqlFixture fixtue, ITestOutputHelper output) : base(output)
        {
            Fixtue = fixtue;
            if (Fixtue.ShouldSkip)
            {
                Skip.Inconclusive("Couldn't connect to: " + ConnectionString + "\n" + fixtue.SkipReason);
            }
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new SQLErrorStore(new ErrorStoreSettings
            {
                ConnectionString = ConnectionString,
                ApplicationName = appName
            }, Fixtue.TableName);
    }

    public class SqlFixture : IDisposable
    {
        public bool ShouldSkip { get; }
        public string SkipReason { get; }
        public string TableName { get; }
        public string TableScript { get; }

        public SqlFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.SQLConnectionString), TestConfig.Current.SQLConnectionString);
            try
            {
                var script = Resource.Get("SqlServer.sql");
                var csb = new SqlConnectionStringBuilder(TestConfig.Current.SQLConnectionString)
                {
                    ConnectTimeout = 2000
                };
                using (var conn = new SqlConnection(csb.ConnectionString))
                {
                    TableName = "Test" + Guid.NewGuid().ToString("N");
                    TableScript = script.Replace("Exceptions", TableName);
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
            using (var conn = new SqlConnection(TestConfig.Current.SQLConnectionString))
            {
                conn.Execute("Drop Table " + TableName);
            }
        }
    }
}
