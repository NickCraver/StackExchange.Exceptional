using System.IO;
#if NETCOREAPP2_0
using System.Reflection;
#endif

namespace StackExchange.Exceptional.Tests
{
    public static class Resource
    {
        public static string Get(string name)
        {
            using (var stream = typeof(Resource)
#if NETCOREAPP2_0
                    .GetTypeInfo()
#endif
                    .Assembly.GetManifestResourceStream("StackExchange.Exceptional.Tests." + name))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }
    }
}
