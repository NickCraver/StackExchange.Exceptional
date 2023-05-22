using System;
using Microsoft.Data.SqlClient;
using System.Runtime.CompilerServices;
using Dapper;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class MicrosoftDataSQLErrorStoreTest : StoreBaseTest, IClassFixture<SqlFixture>
    {
        public string ConnectionString => TestConfig.Current.SQLServerConnectionString;
        private SqlFixture Fixture { get; }

        public MicrosoftDataSQLErrorStoreTest(SqlFixture fixture, ITestOutputHelper output) : base(output)
        {
            Fixture = fixture;
            if (Fixture.ShouldSkip)
            {
                Skip.Inconclusive("Couldn't test against: " + ConnectionString + "\n" + fixture.SkipReason);
            }
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new MicrosoftDataSQLErrorStore(new ErrorStoreSettings
            {
                ConnectionString = ConnectionString,
                ApplicationName = appName,
                TableName = Fixture.TableName
            });

        [Fact]
        public void StoreName()
        {
            const string appName = "TestNameBlarghy";
            var store = new MicrosoftDataSQLErrorStore("Server=.;Trusted_Connection=True;", appName);

            Assert.Equal(appName, store.ApplicationName);
            Statics.Settings = new TestSettings(store);

            Assert.Equal(appName, Statics.Settings.DefaultStore.ApplicationName);
        }
    }

    public class SqlFixture : IDisposable
    {
        public bool ShouldSkip { get; }
        public string SkipReason { get; }
        public string TableName { get; }
        public string TableScript { get; }
        public string ConnectionString { get; }

        public SqlFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.SQLServerConnectionString), TestConfig.Current.SQLServerConnectionString);
            try
            {
                var script = Resource.Get("Scripts.SqlServer.sql");
                var csb = new SqlConnectionStringBuilder(TestConfig.Current.SQLServerConnectionString)
                {
                    ConnectTimeout = 2
                };
                ConnectionString = csb.ConnectionString;
                using var conn = new SqlConnection(ConnectionString);
                TableName = "Test" + Guid.NewGuid().ToString("N");
                TableScript = script.Replace("Exceptions", TableName);
                conn.Execute(TableScript);
            }
            catch (Exception e)
            {
                e.MaybeLog(TestConfig.Current.SQLServerConnectionString);
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        public void Dispose()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Execute("Drop Table " + TableName);
            }
            catch when (ShouldSkip)
            {
                // if we didn't error initially then we'll throw
            }
        }
    }
}
