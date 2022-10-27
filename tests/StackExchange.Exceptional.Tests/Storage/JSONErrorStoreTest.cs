using System.IO;
using System.Runtime.CompilerServices;
using StackExchange.Exceptional.Stores;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public class JSONErrorStoreTest : StoreBaseTest
    {
        protected override bool StoreHardDeletes => true;

        public JSONErrorStoreTest(ITestOutputHelper output) : base(output)
        {
        }

        protected override ErrorStore GetStore([CallerMemberName]string appName = null) =>
            new JSONErrorStore(new ErrorStoreSettings
            {
                ApplicationName = appName,
                Path = GetUniqueFolder(),
                CreatePathIfMissing = true
            });

        protected string GetUniqueFolder() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }
}
