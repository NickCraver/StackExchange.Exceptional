using System;
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
    public class Static : AspNetCoreTest
    {
        public Static(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task ExceptionalStaticSet()
        {
            var appName = Guid.NewGuid().ToString();
            ExceptionalSettings settings = null;
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services.AddExceptional(s =>
                {
                    settings = s;
                    settings.Store.ApplicationName = appName;
                }))
                .Configure(app => app.UseExceptional().Run(context => context.Response.WriteAsync("Hello World")));

            using (var server = new TestServer(builder))
            {
                using (var response = await server.CreateClient().GetAsync("").ConfigureAwait(false))
                {
                    Assert.Same(settings.Store.ApplicationName, Exceptional.Settings.Store.ApplicationName);
                }
            }
        }
    }
}
