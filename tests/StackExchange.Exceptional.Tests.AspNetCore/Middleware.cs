using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    [Collection(NonParallel)]
    public class Middleware : AspNetCoreTest
    {
        public Middleware(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task LogsExceptions()
        {
            using (var server = GetServer(_ => throw new Exception("Log me!")))
            {
                var ex = await Assert.ThrowsAsync<Exception>(async () => await server.CreateClient().GetAsync("").ConfigureAwait(false)).ConfigureAwait(false);
                Assert.Equal("Log me!", ex.Message);

                var errors = await GetErrors().ConfigureAwait(false);
                Assert.Single(errors);
                Assert.Equal("Log me!", errors[0].Message);
            }
        }

        [Fact]
        public async Task RendersExceptionalPage()
        {
            ExceptionalSettings settings = null;
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services.AddExceptional(s =>
                {
                    s.Store.ApplicationName = nameof(RendersExceptionalPage);
                    s.UseExceptionalPageOnThrow = true;
                    settings = s;
                }))
                .Configure(app =>
                {
                    app.UseExceptional();
                    app.Run(_ => throw new Exception("Log me!"));
                });
            using (var server = new TestServer(builder))
            {
                using (var response = await server.CreateClient().GetAsync("").ConfigureAwait(false))
                {
                    Assert.False(response.IsSuccessStatusCode);
                    Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
                    var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Assert.Contains("var Exception = ", responseText);
                    Assert.Contains("An error was thrown during this request.", responseText);
                    Assert.Contains("Log me!", responseText);
                }

                var errors = await settings.DefaultStore.GetAllAsync(nameof(RendersExceptionalPage)).ConfigureAwait(false);
                Assert.Single(errors);
                Assert.Equal("Log me!", errors[0].Message);
            }
        }
    }
}
