using System;
using System.Runtime.CompilerServices;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class OracleErrorStoreTest : StoreBaseTest, IClassFixture<OracleFixture>
    {
        public string ConnectionString => TestConfig.Current.OracleConnectionString;
        private OracleFixture Fixture { get; }

        public OracleErrorStoreTest(OracleFixture fixture, ITestOutputHelper output) : base(output)
        {
            Fixture = fixture;
            if (Fixture.ShouldSkip)
            {
                Skip.Inconclusive("Couldn't test against: " + ConnectionString + "\n" + fixture.SkipReason);
            }
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new OracleErrorStore(new ErrorStoreSettings
            {
                ConnectionString = ConnectionString,
                ApplicationName = appName,
                TableName = Fixture.TableName
            });
    }

    public class OracleFixture : IDisposable
    {
        public bool ShouldSkip { get; }
        public string SkipReason { get; }
        public string TableName { get; }
        public string TableScript { get; }
        private string ConnectionString { get; }

        public OracleFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.OracleConnectionString), TestConfig.Current.OracleConnectionString);
            try
            {
                var script = Resource.Get("Scripts.Oracle.sql");
                var csb = new OracleConnectionStringBuilder(TestConfig.Current.OracleConnectionString)
                {
                    ConnectionTimeout = 2000
                };
                ConnectionString = csb.ConnectionString;
                using (var conn = new OracleConnection(ConnectionString))
                {
                    TableName = "Test" + Guid.NewGuid().ToString("N").Substring(24);
                    TableScript = script.Replace("Exceptions", TableName);

                    //we have to split the script
                    foreach (var scriptPart in TableScript.Split('/'))
                    {
                        conn.Execute(scriptPart);
                    }
                }
            }
            catch (Exception e)
            {
                e.MaybeLog(TestConfig.Current.OracleConnectionString);
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        public void Dispose()
        {
            try
            {
                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Execute("Drop Table " + TableName.ToUpper());
                }
            }
            catch when (ShouldSkip)
            {
                // if we didn't error initially then we'll throw
            }
        }
    }
}
