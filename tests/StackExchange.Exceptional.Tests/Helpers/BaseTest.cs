using StackExchange.Exceptional.Internal;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests
{
    public abstract class BaseTest
    {
        public const string NonParallel = nameof(NonParallel);

        protected ITestOutputHelper Output { get; }

        protected BaseTest(ITestOutputHelper output)
        {
            Output = output;
        }
    }

    public class TestSettings : ExceptionalSettingsBase
    {
        public TestSettings(ErrorStore store)
        {
            DefaultStore = store;
        }
    }

    [CollectionDefinition(BaseTest.NonParallel, DisableParallelization = true)]
    public class NonParallelDefinition
    {
    }
}
