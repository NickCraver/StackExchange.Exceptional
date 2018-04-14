using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests
{
    public class LegacyCompatTest : BaseTest
    {
        public LegacyCompatTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SQLSetter()
        {
            var error = new Error
            {
#pragma warning disable CS0618 // Type or member is obsolete
                SQL = "Select * From Table"
#pragma warning restore CS0618 // Type or member is obsolete
            };
            Assert.Single(error.Commands);
            Assert.Equal("Select * From Table", error.Commands[0].CommandString);
            Assert.Equal("SQL Server Query", error.Commands[0].Type);
        }

        [Fact]
        public void SQLSetterJson()
        {
            var error = Error.FromJson(@"{ ""SQL"": ""Select 1"" }");

            Assert.Single(error.Commands);
            Assert.Equal("Select 1", error.Commands[0].CommandString);
            Assert.Equal("SQL Server Query", error.Commands[0].Type);
        }

        [Fact]
        public void UrlSetterJson()
        {
            var error = Error.FromJson(@"{ ""Url"": ""/path/thing/blah2"" }");

            Assert.Equal("/path/thing/blah2", error.UrlPath);
        }
    }
}
