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
        public void FullBinding()
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

            Assert.Equal("Samples (ASP.NET Core)", settings.Store.ApplicationName);
            Assert.Equal("Memory", settings.Store.Type);
            Assert.Equal(TimeSpan.FromMinutes(5), settings.Store.RollupPeriod);
            Assert.Equal(100, settings.Store.BackupQueueSize);

            // TODO: Full list + Regex checks
        }
    }
}
