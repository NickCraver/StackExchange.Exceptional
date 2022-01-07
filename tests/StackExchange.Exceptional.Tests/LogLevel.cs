using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests
{
    public class LogLevel : BaseTest
    {
        public LogLevel(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void NothingSetIfDefaultsNotEnabledAndNothingSetOnException()
        {
            var ex = Assert.Throws<Exception>(Act(() => throw new Exception("FAIL")));
            Assert.Null(ex.TryToGetLogLevel());
        }

        [Fact]
        public void LevelSetByDefaultsIfEnabledAndNothingSetOnException()
        {
            var ex = Assert.Throws<Exception>(Act(() => throw new Exception("FAIL")));
            Assert.Equal(ExceptionLogLevel.Critical, ex.TryToGetLogLevel());
        }

        [Fact]
        public void LevelSetOnExceptionWhereDefaultsNotEnabled()
        {
            var ex = Assert.Throws<Exception>(Act(() => throw new Exception("FAIL").Warning()));
            Assert.Equal(ExceptionLogLevel.Warning, ex.TryToGetLogLevel());
        }

        [Fact]
        public void LevelSetOnExceptionOverridesDefaults()
        {
            var ex = Assert.Throws<Exception>(Act(() => throw new Exception("FAIL").Warning()));
            Assert.Equal(ExceptionLogLevel.Warning, ex.TryToGetLogLevel());
        }

        [Fact]
        public void LevelSetOnExceptionWhenCaughtAndRethtown()
        {
            var ex = Assert.Throws<Exception>(Act(() =>
            {
                try
                {
                    throw new Exception("FAIL");
                }
                catch (Exception ex)
                {
                    ex.Warning();
                    throw;
                }
            }));
            Assert.Equal(ExceptionLogLevel.Warning, ex.TryToGetLogLevel());
        }

        /// <summary>
        /// When an exception is thrown with one log level, it may be caught and re-thrown with a different log level
        /// </summary>
        [Fact]
        public void LevelOnExceptionMayBeOverriddenByCatchingAndRethrowing()
        {
            var ex = Assert.Throws<Exception>(Act(() =>
            {
                try
                {
                    throw new Exception("FAIL").Debug();
                }
                catch (Exception ex)
                {
                    ex.Info();
                    throw;
                }
            }));
            Assert.Equal(ExceptionLogLevel.Info, ex.TryToGetLogLevel());
        }

        /// <summary>
        /// When an AggregateException is thrown, we want the exception(s) that are wrapped in that type to be logged but to log the AggregateException itself as well
        /// would be duplication and add noise to the logs - so the ApplyDefaultLevelToAllThrowExceptions logic should not automatically apply a log level to aggregate
        /// exceptions (but it SHOULD apply a log level to the exception(s) wrapped by it)
        /// </summary>
        [Fact]
        public void DefaultLevelAppliedToExceptionInAggregateExceptionButNotTheWrapper()
        {
            // Note: Use Task.WaitAll to get an AggregateException raise (Task.WhenAll will unwrap the AggregateException to make async/await code tidier)
            var ex = Assert.Throws<AggregateException>(() => Task.WaitAll(GetFailingTask()));
            Assert.Null(ex.TryToGetLogLevel());
            Assert.Equal(ExceptionLogLevel.Critical, ex.InnerException.TryToGetLogLevel());

            static async Task<object> GetFailingTask()
            {
                await Task.Yield();
                throw new Exception("FAIL");
            }
        }

        [Fact]
        public void LevelSetOnExceptionOverridesDefaultOnAggregateExceptionInnerException()
        {
            var ex = Assert.Throws<AggregateException>(() => Task.WaitAll(GetFailingTask()));
            Assert.Equal(ExceptionLogLevel.Warning, ex.InnerException.TryToGetLogLevel());

            static async Task<object> GetFailingTask()
            {
                await Task.Yield();
                throw new Exception("FAIL").Warning();
            }
        }

        /// <summary>
        /// TaskCanceledException should not have a log level applied by the ApplyDefaultLevelToAllThrowExceptions logic because it indicates control flow rather
        /// than a true 'exception' because logic elsewhere determines when / how / whether to cancel a task
        /// </summary>
        [Fact]
        public async Task NothingSetOnTaskCanceledException()
        {
            var cts = new CancellationTokenSource(delay: TimeSpan.FromMilliseconds(100));
            var task = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);

            var ex = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.Null(ex.TryToGetLogLevel());
        }

        /// <summary>
        /// The Assert.Throws method overload that is matched for an exception-throwing lambda is the one that takes a Func that returns a Task
        /// and we'll get an analyser warning that Assert.ThrowsAsync should be used - wrapping the lamba in this causes Assert.Throw to match
        /// the overload that takes an Action, which prevents that warning from being generated by the xUnit analyser
        /// </summary>
        static Action Act(Action action) => action;
    }
}
