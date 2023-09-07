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
            if (TestConfig.Current.SQLServerConnectionString.IsNullOrEmpty())
            {
                Skip.Inconclusive("SQLConnectionString config is missing, unable to test.");
            }

            const string appName = "AppNameViaConfig";
            Exceptional.Configure(settings => settings.DefaultStore = new MicrosoftDataSQLErrorStore(TestConfig.Current.SQLServerConnectionString, appName));

            Assert.Equal(appName, Exceptional.Settings.DefaultStore.ApplicationName);
            Assert.Equal(appName, Statics.Settings.DefaultStore.ApplicationName);

            var error = new Exception().GetErrorIfNotIgnored(Statics.Settings);
            Assert.Equal(appName, error.ApplicationName);
        }
    }
}
