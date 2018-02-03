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
            if (TestConfig.Current.SQLConnectionString.IsNullOrEmpty())
            {
                Skip.Inconclusive("SQLConnectionString config is missing, unable to test.");
            }

            const string appName = "AppNameViaConfig";
            Exceptional.Configure(settings => settings.DefaultStore = new SQLErrorStore(TestConfig.Current.SQLConnectionString, appName));

            Assert.Equal(Exceptional.Settings.DefaultStore.ApplicationName, appName);
            Assert.Equal(Statics.Settings.DefaultStore.ApplicationName, appName);

            var error = new Exception().GetErrorIfNotIgnored(Statics.Settings);
            Assert.Equal(appName, error.ApplicationName);
        }
    }
}
