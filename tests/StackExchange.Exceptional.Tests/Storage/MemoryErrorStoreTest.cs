using System.Runtime.CompilerServices;
using StackExchange.Exceptional.Stores;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class MemoryErrorStoreTest : StoreBaseTest
    {
        protected override bool StoreHardDeletes => true;

        public MemoryErrorStoreTest(ITestOutputHelper output) : base(output)
        {
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new MemoryErrorStore(new ErrorStoreSettings
            {
                ApplicationName = appName
            });
    }
}
