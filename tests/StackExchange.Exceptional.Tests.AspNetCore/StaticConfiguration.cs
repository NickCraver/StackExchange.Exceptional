using System;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    [Collection(NonParallel)]
    public class StaticConfiguration : AspNetCoreTest
    {
        public StaticConfiguration(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AppNameViaConfigure()
        {
            const string appName = "AppNameViaConfig";
            Exceptional.Configure(settings => settings.DefaultStore = new SQLErrorStore("Server=.", appName));

            Assert.Equal(Exceptional.Settings.DefaultStore.ApplicationName, appName);
            Assert.Equal(Statics.Settings.DefaultStore.ApplicationName, appName);

            var e = new Exception().LogNoContext();
            Assert.Equal(appName, e.ApplicationName);
        }
    }
}
