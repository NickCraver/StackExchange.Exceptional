using System;
using System.Runtime.CompilerServices;
using MongoDB.Driver;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class MongoDBErrorStoreTest : StoreBaseTest, IClassFixture<MongoDBFixture>
    {
        private MongoDBFixture Fixture { get; }

        public MongoDBErrorStoreTest(MongoDBFixture fixture, ITestOutputHelper output) : base(output)
        {
            Fixture = fixture;
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new MongoDBErrorStore(new ErrorStoreSettings
            {
                ConnectionString = Fixture.ConnectionString,
                ApplicationName = appName,
                TableName = Fixture.TableName
            });
    }

    public class MongoDBFixture : IDisposable
    {
        public string ConnectionString { get; }
        public string TableName { get; }

        public MongoDBFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.MongoDBConnectionString), TestConfig.Current.MongoDBConnectionString);
            ConnectionString = TestConfig.Current.MongoDBConnectionString;
            TableName = "Test" + Guid.NewGuid().ToString("N").Substring(24);
        }

        public void Dispose()
        {
            var databaseName = new MongoUrl(TestConfig.Current.MongoDBConnectionString).DatabaseName;
            var collection = new MongoClient(TestConfig.Current.MongoDBConnectionString).GetDatabase(databaseName);
            collection.DropCollection(TableName);
        }
    }
}
