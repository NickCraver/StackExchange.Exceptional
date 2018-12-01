using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using Dapper;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class SQLErrorStoreTest : StoreBaseTest, IClassFixture<SqlFixture>
    {
        public string ConnectionString => TestConfig.Current.SQLConnectionString;
        private SqlFixture Fixture { get; }

        public SQLErrorStoreTest(SqlFixture fixture, ITestOutputHelper output) : base(output)
        {
            Fixture = fixture;
            if (Fixture.ShouldSkip)
            {
                Skip.Inconclusive("Couldn't test against: " + ConnectionString + "\n" + fixture.SkipReason);
            }
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new SQLErrorStore(new ErrorStoreSettings
            {
                ConnectionString = ConnectionString,
                ApplicationName = appName,
                TableName = Fixture.TableName
            });

        [Fact]
        public void StoreName()
        {
            const string appName = "TestNameBlarghy";
            var store = new SQLErrorStore("Server=.;Trusted_Connection=True;", appName);

            Assert.Equal(store.ApplicationName, appName);
            Statics.Settings = new TestSettings(store);

            Assert.Equal(Statics.Settings.DefaultStore.ApplicationName, appName);
        }
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
                var script = Resource.Get("Scripts.SqlServer.sql");
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
            try
            {
                using (var conn = new SqlConnection(TestConfig.Current.SQLConnectionString))
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
