using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public abstract class AspNetCoreTest : BaseTest
    {
        protected ExceptionalSettings CurrentSettings { get; set; }

        protected AspNetCoreTest(ITestOutputHelper output) : base(output) { }

        protected string LogName([CallerMemberName]string name = null) => name;

        protected Task<List<Error>> GetErrors() => CurrentSettings.DefaultStore.GetAllAsync();

        protected TestServer GetServer(RequestDelegate requestDelegate, [CallerMemberName]string name = null) =>
            new TestServer(BasicBuilder(requestDelegate, name));

        protected IWebHostBuilder BasicBuilder(RequestDelegate requestDelegate, [CallerMemberName]string name = null) =>
            new WebHostBuilder()
               .ConfigureServices(services => services.AddExceptional(s =>
               {
                   s.DefaultStore = new MemoryErrorStore();
                   CurrentSettings = s;
               }))
               .Configure(app =>
               {
                   app.UseExceptional();
                   app.Run(requestDelegate);
               });
    }

    [CollectionDefinition(BaseTest.NonParallel, DisableParallelization = true)]
    public class NonParallelDefinition
    {
    }
}
