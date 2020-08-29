using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    [Collection(NonParallel)]
    public class StartupFilter : AspNetCoreTest
    {
        public StartupFilter(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task CapturesStartupError()
        {
            var startupEx = Assert.Throws<Exception>(() => new TestServer(
                    new WebHostBuilder()
                        .SuppressStatusMessages(true) // Prevent log spam in tests
                        .ConfigureServices(services => services.AddExceptional(s =>
                        {
                            s.DefaultStore = new MemoryErrorStore();
                            CurrentSettings = s;
                        }))
                        .Configure(app =>
                        {
                            new Exception("Startup log").LogNoContext();
                            throw new Exception("A-a-a-a-a-a-app killer!");
                            // Unreachable code...
                            //app.UseExceptional();
                            //app.Run(r => throw new Exception("Log me!"));
                        })));
            
            Assert.Equal("A-a-a-a-a-a-app killer!", startupEx.Message);

            var errors = await GetErrorsAsync();
            // When we hook up ILogger<WebHost>, we can capture non-direct logs in .Configure() here:
            // https://github.com/dotnet/aspnetcore/blob/01e05359d644a1f68c1e26a196fc3370ec9ded49/src/Hosting/Hosting/src/Internal/WebHost.cs#L234-L240
            Assert.Single(errors);
            Assert.Equal("Startup log", errors[0].Message);
            //Assert.Equal("A-a-a-a-a-a-app killer!", errors[1].Message);
        }
    }
}
