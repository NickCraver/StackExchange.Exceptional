using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public class Configuration : AspNetCoreTest
    {
        public Configuration(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void UsingBindOverride()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"Configs\Full.json")
                .Build();

            Assert.NotNull(config);
            var exceptionalSection = config.GetSection("Exceptional");
            Assert.NotNull(exceptionalSection);
            Assert.Equal("Exceptional", exceptionalSection.Key);

            var settings = new ExceptionalSettings();
            exceptionalSection.Bind(settings);

            // Top level
            Assert.NotNull(settings.DataIncludeRegex);
            Assert.Matches(settings.DataIncludeRegex, "MyPrefix.Test");
            Assert.True(settings.UseExceptionalPageOnThrow);

            // Store
            Assert.NotNull(settings.Store);
            Assert.Equal("Samples (ASP.NET Core)", settings.Store.ApplicationName);
            Assert.Equal("Memory", settings.Store.Type);
            Assert.Equal(TimeSpan.FromMinutes(5), settings.Store.RollupPeriod);
            Assert.Equal(100, settings.Store.BackupQueueSize);

            // Ignore
            Assert.NotNull(settings.Ignore);
            Assert.Equal(2, settings.Ignore.Regexes.Count);
            Assert.Contains(settings.Ignore.Regexes, r => r.IsMatch("Request timed out."));
            Assert.Contains(settings.Ignore.Regexes, r => r.IsMatch("Top SECRET DATA."));
            Assert.DoesNotContain(settings.Ignore.Regexes, r => r.IsMatch("Pickles"));
            Assert.Equal(2, settings.Ignore.Types.Count);
            Assert.Contains("MyNameSpace.MyException", settings.Ignore.Types);
            Assert.Contains("MyNameSpace.NoLogPleaseException", settings.Ignore.Types);

            // LogFilters
            Assert.NotNull(settings.LogFilters);
            Assert.Single(settings.LogFilters.Cookie);
            Assert.Equal("**no tokens saved! pheww**", settings.LogFilters.Cookie["authToken"]);
            Assert.Single(settings.LogFilters.Form);
            Assert.Equal("*********", settings.LogFilters.Form["password"]);
            Assert.Single(settings.LogFilters.Header);
            Assert.Equal("*********", settings.LogFilters.Header["Accept-Language"]);
            Assert.Equal("*********", settings.LogFilters.Header["ACCEPT-language"]);
            Assert.Single(settings.LogFilters.QueryString);
            Assert.Equal("**no tokens saved! pheww**", settings.LogFilters.QueryString["queryToken"]);
            Assert.Equal("**no tokens saved! pheww**", settings.LogFilters.QueryString["QUERYToken"]);

            // Email
            Assert.NotNull(settings.Email);
            Assert.Equal("tester@example.com", settings.Email.ToAddress);
            Assert.Equal("exceptions@test.com", settings.Email.FromAddress);
            Assert.Equal("Wendy", settings.Email.FromDisplayName);
            Assert.Equal("localhost", settings.Email.SMTPHost);
            Assert.Equal(25, settings.Email.SMTPPort);
            Assert.Equal("dummy", settings.Email.SMTPUserName);
            Assert.Equal("pwd", settings.Email.SMTPPassword);
            Assert.True(settings.Email.SMTPEnableSSL);
            Assert.True(settings.Email.PreventDuplicates);
        }

        [Fact]
        public void JSONStorage()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"Configs\Storage.JSON.json")
                .Build();

            Assert.NotNull(config);
            var exceptionalSection = config.GetSection("Exceptional");
            Assert.NotNull(exceptionalSection);

            var settings = new ExceptionalSettings();
            exceptionalSection.Bind(settings);

            // Store
            Assert.NotNull(settings.Store);
            Assert.Equal("Samples (ASP.NET Core JSON)", settings.Store.ApplicationName);
            Assert.Equal("JSON", settings.Store.Type);
            Assert.Equal("/errors", settings.Store.Path);
            Assert.Equal(200, settings.Store.Size);

            Assert.IsType<JSONErrorStore>(settings.DefaultStore);
            var jsonStore = settings.DefaultStore as JSONErrorStore;
            Assert.Equal("Samples (ASP.NET Core JSON)", jsonStore.ApplicationName);
        }

        [Fact]
        public void SQLStorage()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"Configs\Storage.SQL.json")
                .Build();

            Assert.NotNull(config);
            var exceptionalSection = config.GetSection("Exceptional");
            Assert.NotNull(exceptionalSection);

            var settings = new ExceptionalSettings();
            exceptionalSection.Bind(settings);

            // Store
            Assert.NotNull(settings.Store);
            Assert.Equal("Samples (ASP.NET Core SQL)", settings.Store.ApplicationName);
            Assert.Equal("SQL", settings.Store.Type);
            Assert.Equal("Server=.;Database=Local.Exceptions;Trusted_Connection=True;", settings.Store.ConnectionString);
            Assert.Equal("MyExceptions", settings.Store.TableName);

            Assert.IsType<SQLErrorStore>(settings.DefaultStore);
            var sqlStore = settings.DefaultStore as SQLErrorStore;
            Assert.Equal("Samples (ASP.NET Core SQL)", sqlStore.ApplicationName);
        }

        [Fact]
        public void MySQLStorage()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"Configs\Storage.MySQL.json")
                .Build();

            Assert.NotNull(config);
            var exceptionalSection = config.GetSection("Exceptional");
            Assert.NotNull(exceptionalSection);

            var settings = new ExceptionalSettings();
            exceptionalSection.Bind(settings);

            // Store
            Assert.NotNull(settings.Store);
            Assert.Equal("Samples (ASP.NET Core MySQL)", settings.Store.ApplicationName);
            Assert.Equal("MySQL", settings.Store.Type);
            Assert.Equal("Server=.;Database=Exceptions;Username=Exceptions;Pwd=myPassword!", settings.Store.ConnectionString);

            Assert.IsType<MySQLErrorStore>(settings.DefaultStore);
            var sqlStore = settings.DefaultStore as MySQLErrorStore;
            Assert.Equal("Samples (ASP.NET Core MySQL)", sqlStore.ApplicationName);
        }

        [Fact]
        public void PostgreSqlStorage()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"Configs\Storage.PostgreSql.json")
                .Build();

            Assert.NotNull(config);
            var exceptionalSection = config.GetSection("Exceptional");
            Assert.NotNull(exceptionalSection);

            var settings = new ExceptionalSettings();
            exceptionalSection.Bind(settings);

            // Store
            Assert.NotNull(settings.Store);
            Assert.Equal("Samples (ASP.NET Core PostgreSql)", settings.Store.ApplicationName);
            Assert.Equal("PostgreSql", settings.Store.Type);
            Assert.Equal("Server=localhost;Port=5432;Database=Exceptions;User Id=postgres;Password=postgres;", settings.Store.ConnectionString);

            Assert.IsType<PostgreSqlErrorStore>(settings.DefaultStore);
            var sqlStore = settings.DefaultStore as PostgreSqlErrorStore;
            Assert.Equal("Samples (ASP.NET Core PostgreSql)", sqlStore.ApplicationName);
        }

        [Fact]
        public void MongoDBStorage()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"Configs\Storage.MongoDB.json")
                .Build();

            Assert.NotNull(config);
            var exceptionalSection = config.GetSection("Exceptional");
            Assert.NotNull(exceptionalSection);

            var settings = new ExceptionalSettings();
            exceptionalSection.Bind(settings);

            // Store
            Assert.NotNull(settings.Store);
            Assert.Equal("Samples (ASP.NET Core MongoDB)", settings.Store.ApplicationName);
            Assert.Equal("MongoDB", settings.Store.Type);
            Assert.Equal("mongodb://localhost/test", settings.Store.ConnectionString);

            Assert.IsType<MongoDBErrorStore>(settings.DefaultStore);
            var store = settings.DefaultStore as MongoDBErrorStore;
            Assert.Equal("Samples (ASP.NET Core MongoDB)", store.ApplicationName);
        }
    }
}
