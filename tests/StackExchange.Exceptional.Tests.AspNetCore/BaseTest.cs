using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
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
}
