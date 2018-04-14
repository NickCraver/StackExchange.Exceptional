using System;
using System.Text.RegularExpressions;
using StackExchange.Exceptional.Internal;
using StackExchange.Exceptional.Stores;
using Xunit;

namespace StackExchange.Exceptional.Tests
{
    public class Ignore
    {
        [Fact]
        public void ShouldRecordType()
        {
            var settings = new TestSettings(new MemoryErrorStore());
            settings.Ignore.Types.Add(typeof(ExcludeType).FullName);

            Assert.False(new Exception().ShouldBeIgnored(settings));
            Assert.True(new ExcludeType().ShouldBeIgnored(settings));
            Assert.True(new ExcludeTypeDescendant().ShouldBeIgnored(settings));
            Assert.False(new ExcludeType2().ShouldBeIgnored(settings));
        }

        [Fact]
        public void ShouldRecordMessage()
        {
            var settings = new TestSettings(new MemoryErrorStore());
            settings.Ignore.Regexes.Add(new Regex("Goobly.*"));

            Assert.False(new Exception().ShouldBeIgnored(settings));
            Assert.False(new Exception("Goo").ShouldBeIgnored(settings));
            Assert.True(new Exception("Goobly Woobly").ShouldBeIgnored(settings));
        }
    }

#pragma warning disable RCS1194 // Implement exception constructors.
    public class ExcludeType : Exception { }
    public class ExcludeTypeDescendant : ExcludeType { }
    public class ExcludeType2 : Exception { }
#pragma warning restore RCS1194 // Implement exception constructors.
}
