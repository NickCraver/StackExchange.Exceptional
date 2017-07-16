using System;
using System.Threading.Tasks;

namespace StackExchange.Exceptional
{
    public static partial class Utils
    {
        /// <summary>
        /// General test methods for exceptions.
        /// </summary>
        public static class Test
        {
#pragma warning disable CS1591
            public static async Task ThrowStackAsync() => await MethodA().ConfigureAwait(false);
            private static async Task MethodA() => await MethodB().ConfigureAwait(false);
            private static async Task MethodB() => await MethodC().ConfigureAwait(false);
#pragma warning disable CS1998
            private static async Task MethodC() {
                var ex = new Exception("This is a test async exception from Exceptional, I SAY GOOD DAY!");
                ex.Data["SQL"] = "Select * From FUBARAsync -- This is a SQL command!";
                ex.Data["Redis-Server"] = "REDIS01";
                ex.Data["Some-Key"] = "Hellooooooooooo";
                throw ex;
            }
#pragma warning restore CS1998
#pragma warning restore CS1591
        }
    }
}
