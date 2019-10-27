using System;
using System.Runtime.CompilerServices;

namespace StackExchange.Exceptional.Tests
{
    public static class TestExtensions
    {
        internal static void MaybeLog(this Exception ex, string connectionString, [CallerFilePath] string file = null, [CallerMemberName] string caller = null)
        {
            if (TestConfig.Current.EnableTestLogging)
            {
                Console.WriteLine($"{file} {caller}: {ex.Message}");
                Console.WriteLine("  " + connectionString);
            }
        }
    }
}
