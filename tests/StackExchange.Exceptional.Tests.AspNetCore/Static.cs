using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    [Collection(NonParallel)]
    public class Static : BaseTest
    {
        public Static(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task ExceptionalStaticSet()
        {
            ExceptionalSettings settings = null;
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services.AddExceptional(s => settings = s))
                .Configure(app => app.UseExceptional().Run(context => context.Response.WriteAsync("Hello World")));

            using (var server = new TestServer(builder))
            {
                using (var response = await server.CreateClient().GetAsync("").ConfigureAwait(false))
                {
                    Assert.Same(Exceptional.Settings, settings);
                }
            }
        }
    }
}
