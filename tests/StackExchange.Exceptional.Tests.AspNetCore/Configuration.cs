using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public class Configuration : BaseTest
    {
        public Configuration(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void UsingBindOverride()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("test.json")
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
    }
}
