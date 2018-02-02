using System;
using System.Collections.Generic;
using StackExchange.Exceptional.Pages;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public class Pages : AspNetCoreTest
    {
        public Pages(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void RendersNullCustomData()
        {
            var settings = new ExceptionalSettings();
            var ex = new Exception("My exception");
            var error = new Error(ex, settings, initialCustomData: new Dictionary<string, string>
            {
                ["MyData"] = null
            });

            var errorPage = new ErrorDetailPage(error, settings, settings.DefaultStore, "/", error.GUID);
            Assert.NotNull(errorPage);

            var rendered = errorPage.Render();
            Assert.NotNull(rendered);
            Assert.Contains("var Exception = ", rendered);
            Assert.Contains("MyData", rendered);
        }
    }
}
