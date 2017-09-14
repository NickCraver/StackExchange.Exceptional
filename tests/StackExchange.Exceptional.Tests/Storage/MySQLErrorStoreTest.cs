using System;
using System.Runtime.CompilerServices;
using Dapper;
using MySql.Data.MySqlClient;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class MySQLErrorStoreTest : StoreBaseTest, IClassFixture<MySqlFixture>
    {
        public string ConnectionString => TestConfig.Current.MySQLConnectionString;
        private MySqlFixture Fixtue { get; }

        public MySQLErrorStoreTest(MySqlFixture fixtue, ITestOutputHelper output) : base(output)
        {
            Fixtue = fixtue;
            if (Fixtue.ShouldSkip)
            {
                Skip.Inconclusive("Couldn't connect to: " + ConnectionString + "\n" + fixtue.SkipReason);
            }
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new MySQLErrorStore(new ErrorStoreSettings
            {
                ConnectionString = ConnectionString,
                ApplicationName = appName
            }, Fixtue.TableName);
    }

    public class MySqlFixture : IDisposable
    {
        public bool ShouldSkip { get; }
        public string SkipReason { get; }
        public string TableName { get; }
        public string TableScript { get; }

        public MySqlFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.MySQLConnectionString), TestConfig.Current.MySQLConnectionString);
            try
            {
                var script = Resource.Get("MySQL.sql");
                var csb = new MySqlConnectionStringBuilder(TestConfig.Current.MySQLConnectionString)
                {
                    ConnectionTimeout = 2000
                };
                using (var conn = new MySqlConnection(csb.ConnectionString))
                {
                    TableName = "Test" + Guid.NewGuid().ToString("N").Substring(24);
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
            try
            {
                using (var conn = new MySqlConnection(TestConfig.Current.MySQLConnectionString))
                {
                    //conn.Execute("Drop Table " + TableName);
                }
            }
            catch when (ShouldSkip)
            {
                // if we didn't error initially then we'll throw
            }
        }
    }
}
