using System;
using StackExchange.Exceptional.Stores;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests
{
    public class Handlers : BaseTest
    {
        public Handlers(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void OuterException()
        {
            var settings = GetHandlerSettings();
            var exception = new ArgumentNullException("param1");
            var error = new Error(exception, settings);

            Assert.NotNull(error.Commands);
            Assert.Single(error.Commands);
            Assert.Equal("Test", error.Commands[0].CommandString);

            var argError = new Error(new ArgumentOutOfRangeException(), settings);

            Assert.NotNull(argError.Commands);
            Assert.Single(argError.Commands);
            Assert.Equal(typeof(ArgumentOutOfRangeException).FullName, argError.Commands[0].CommandString);
        }

        [Fact]
        public void InnerException()
        {
            var exception = new Exception("Test", new ArgumentNullException("param1"));
            var settings = GetHandlerSettings();
            var error = new Error(exception, settings);

            Assert.NotNull(error.Commands);
            Assert.Single(error.Commands);
            Assert.Equal("Test", error.Commands[0].CommandString);

            var argError = new Error(new Exception("Test", new ArgumentOutOfRangeException()), settings);

            Assert.NotNull(argError.Commands);
            Assert.Single(argError.Commands);
            Assert.Equal(typeof(ArgumentOutOfRangeException).FullName, argError.Commands[0].CommandString);
        }

        private TestSettings GetHandlerSettings()
        {
            var settings = new TestSettings(new MemoryErrorStore());
            settings.ExceptionActions.Clear();
            // Generic handler helper
            settings.ExceptionActions.AddHandler<ArgumentNullException>((e, ex) =>
            {
                e.AddCommand(
                    new Command("Test", "Test")
                        .AddData("ParamName", ex.ParamName));
            });
            // String helper
            settings.ExceptionActions.AddHandler(typeof(ArgumentOutOfRangeException).FullName, (e, ex) =>
            {
                e.AddCommand(new Command("Type", ex.GetType().FullName));
            });
            return settings;
        }
    }
}
