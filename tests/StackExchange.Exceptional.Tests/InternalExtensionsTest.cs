using System;
using System.IO;
using StackExchange.Exceptional.Internal;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests
{
    public class InternalExtensionsTest : BaseTest
    {
        public InternalExtensionsTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ShouldBeIgnored()
        {
            var settings = new TestSettings(null);

            Assert.False(new Exception().ShouldBeIgnored(settings));
            Assert.False(new ArgumentNullException().ShouldBeIgnored(settings));

            settings.Ignore.Types.Add(typeof(Exception).FullName);
            Assert.True(new Exception().ShouldBeIgnored(settings));
            Assert.True(new ArgumentNullException().ShouldBeIgnored(settings));

            settings.Ignore.Types.Clear();

            settings.Ignore.Types.Add(typeof(ArgumentNullException).FullName);
            Assert.False(new Exception().ShouldBeIgnored(settings));
            Assert.True(new ArgumentNullException().ShouldBeIgnored(settings));

            // FileLoadException inherits from IOException
            Assert.False(new FileLoadException().ShouldBeIgnored(settings));
            settings.Ignore.Types.Add(typeof(IOException).FullName);
            Assert.True(new FileLoadException().ShouldBeIgnored(settings));
        }

        [Fact]
        public void IsDescendentOf()
        {
            Assert.True(typeof(ArgumentNullException).IsDescendentOf(typeof(Exception).FullName));
            Assert.True(typeof(AccessViolationException).IsDescendentOf(typeof(Exception).FullName));
            Assert.False(typeof(Exception).IsDescendentOf(typeof(ArgumentNullException).FullName));

            // FileLoadException inherits from IOException
            Assert.True(typeof(FileLoadException).IsDescendentOf(typeof(IOException).FullName));
            Assert.True(typeof(FileLoadException).IsDescendentOf(typeof(Exception).FullName));
            Assert.True(typeof(IOException).IsDescendentOf(typeof(Exception).FullName));

            Assert.False(typeof(IOException).IsDescendentOf(typeof(FileLoadException).FullName));
        }
    }
}
