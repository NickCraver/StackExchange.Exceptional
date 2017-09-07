using Microsoft.Extensions.Configuration;
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
            var section = config.GetSection("Exceptional");
            Assert.NotNull(section);
            Assert.Equal("Exceptional", section.Key);

            var settings = new ExceptionalSettings();
            config.Bind(settings);
            Assert.Equal(settings.Store.ApplicationName, $"Samples (ASP.NET Core)");
        }
    }
}
