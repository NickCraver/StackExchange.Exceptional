using System;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public class Events : AspNetCoreTest
    {
        public Events(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void BeforeAndAfter()
        {
            bool beforeFired = false,
                 afterFired = false;
            Error error = null;
            var settings = new ExceptionalSettings();
            settings.OnBeforeLog += (s, e) => beforeFired = true;
            settings.OnAfterLog += (s, e) => { afterFired = true; error = e.Error; };

            var ex = new Exception();
            var err = new Error(ex, settings);
            Assert.False(beforeFired);
            Assert.False(afterFired);

            Assert.True(err.LogToStore());
            Assert.True(beforeFired);
            Assert.True(afterFired);
            Assert.NotNull(error);
            Assert.Same(ex, error.Exception);
        }

        [Fact]
        public void BeforeAbort()
        {
            bool beforeFired = false,
                 afterFired = false;
            Error error = null;
            var settings = new ExceptionalSettings();
            settings.OnBeforeLog += (s, e) => { beforeFired = true; e.Abort = true; };
            settings.OnAfterLog += (s, e) => { afterFired = true; error = e.Error; };

            var ex = new Exception();
            var err = new Error(ex, settings);
            Assert.False(beforeFired);
            Assert.False(afterFired);

            Assert.False(err.LogToStore());
            Assert.True(beforeFired);
            Assert.False(afterFired);
            Assert.Null(error);
        }
    }
}
