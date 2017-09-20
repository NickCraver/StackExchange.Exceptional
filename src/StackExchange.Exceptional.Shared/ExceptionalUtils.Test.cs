using System;
using System.Threading.Tasks;

namespace StackExchange.Exceptional
{
    public static partial class ExceptionalUtils
    {
        /// <summary>
        /// General test methods for exceptions.
        /// </summary>
        public static class Test
        {
#pragma warning disable CS1591
#pragma warning disable CS1998
            public static async Task ThrowStackAsync() => await MethodA().ConfigureAwait(false);
            private static async Task MethodA() => await MethodB().ConfigureAwait(false);
            private static async Task MethodB() => await MethodC().ConfigureAwait(false);
            private static async Task MethodC() {
                var ex = new Exception("This is a test async exception from Exceptional, I SAY GOOD DAY!");
                ex.Data["SQL"] = "Select * From FUBARAsync -- This is a SQL command!";
                ex.Data["Redis-Server"] = "REDIS01";
                ex.Data["Some-Key"] = "Hello!";
                throw ex;
            }
#pragma warning restore CS1998
#pragma warning restore CS1591

            /// <summary>
            /// Gets an example RedisException
            /// </summary>
            /// <returns></returns>
            public static RedisException GetRedisException()
            {
                var ex = new RedisException("GetInt: Timeout performing GET prod-throttle-11.22.33.44-reqs, inst: 1, queue: 8, qu: 0, qs: 8, qc: 0, wr: 0, wq: 0, in: 0, ar: 0, clientName: API v2, serverEndpoint: Unspecified/redis01.servers.stackexchange.com:6379, keyHashSlot: 2576, IOCP: (Busy=0,Free=1000,Min=48,Max=1000), WORKER: (Busy=3,Free=32764,Min=48,Max=32767) (Please take a look at this article for some common client-side issues that can cause timeouts: http://stackexchange.github.io/StackExchange.Redis/Timeouts) with keys: throttle-11.22.33.44-reqs");
                ex.Data["redis-command"] = "GET prod-throttle-11.22.33.44-reqs";
                ex.Data["redis-server"] = "redis01.servers.stackexchange.com:6379";
                ex.Data["Redis-Message"] = "GET prod-throttle-11.22.33.44-reqs";
                ex.Data["Redis-Instantaneous"] = "1";
                ex.Data["Redis-Queue-Length"] = "8";
                ex.Data["Redis-Queue-Outstanding"] = "0";
                ex.Data["Redis-Queue-Awaiting-Response"] = "0";
                ex.Data["Redis-Queue-Completion-Outstanding"] = "0";
                ex.Data["Redis-Active-Writers"] = "0";
                ex.Data["Redis-Write-Queue"] = "0";
                ex.Data["Redis-Inbound-Bytes"] = "0";
                ex.Data["Redis-Active-Readers"] = "0";
                ex.Data["Redis-Client-Name"] = "API v2";
                ex.Data["Redis-Server-Endpoint"] = "Unspecified/redis01.servers.stackexchange.com:6379";
                ex.Data["Redis-Key-HashSlot"] = "2576";
                ex.Data["Redis-ThreadPool-IO-Completion"] = "(Busy=0,Free=1000,Min=48,Max=1000)";
                ex.Data["Redis-ThreadPool-Workers"] = "(Busy=3,Free=32764,Min=48,Max=32767)";
                ex.Data["Redis-Busy-Workers"] = "3";
                return ex;
            }

#pragma warning disable RCS1194 // Implement exception constructors.
            /// <summary>
            /// A mock RedisException, like StackExchange.Redis contains
            /// </summary>
            public class RedisException : Exception, IExceptionalHandled
            {
                /// <summary>
                /// Creates a redis exception with a message.
                /// </summary>
                /// <param name="message">The message to use for this exception.</param>
                public RedisException(string message) : base(message) { }

                /// <summary>
                /// An example of a handler that adds data to the exception when logged.
                /// This will be called whenever the exception is logged, adding a command and keys
                /// to the exception.
                /// </summary>
                /// <param name="e">The <see cref="Error"/> wrapper of the exception to handle.</param>
                public void ExceptionalHandler(Error e)
                {
                    var cmd = e.AddCommand(new Command("Redis"));
                    foreach (string k in e.Exception.Data.Keys) // can also use `this`
                    {
                        var val = e.Exception.Data[k] as string;
                        if (k == "redis-command") cmd.CommandString = val;
                        if (k.StartsWith("Redis-")) cmd.AddData(k.Substring("Redis-".Length), val);
                    }
                }
            }
#pragma warning restore RCS1194 // Implement exception constructors.
        }
    }
}
