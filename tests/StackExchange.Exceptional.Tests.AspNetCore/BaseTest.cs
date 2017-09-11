using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public abstract class BaseTest
    {
        public const string NonParallel = nameof(NonParallel);
        protected ExceptionalSettings CurrentSettings { get; set; }

        protected ITestOutputHelper Output { get; }

        protected BaseTest(ITestOutputHelper output)
        {
            Output = output;
        }

        protected string LogName([CallerMemberName]string name = null) => name;

        protected TestServer GetServer(RequestDelegate requestDelegate, [CallerMemberName]string name = null) =>
            new TestServer(BasicBuilder(requestDelegate, name));

        protected IWebHostBuilder BasicBuilder(RequestDelegate requestDelegate, [CallerMemberName]string name = null) =>
            new WebHostBuilder()
               .ConfigureServices(services => services.AddExceptional(s =>
               {
                   s.Store.ApplicationName = name;
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
