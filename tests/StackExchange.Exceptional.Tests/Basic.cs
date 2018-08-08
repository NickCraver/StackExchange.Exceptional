using System;
using Xunit;

namespace StackExchange.Exceptional.Tests
{
    public class Basic
    {
        [Fact]
        public void NullNoLog()
        {
            Assert.Null(((Exception)null).LogNoContext());
        }
    }
}
