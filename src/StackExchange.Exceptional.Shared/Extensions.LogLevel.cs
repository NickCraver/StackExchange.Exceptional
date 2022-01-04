using System;
using System.Runtime.ExceptionServices;
​
namespace StackExchange.Exceptional
{
    public static partial class Extensions
    {
        const string LogLevelKey = "ExceptionLogging.Level";
​
        public static void ApplyDefaultLevelToAllThrowExceptions() => AppDomain.CurrentDomain.FirstChanceException += FirstThrown;
​
        /// <summary>
        /// Should only need to call this from unit tests, so set as internal and use InternalsVisibleTo to make it available to that code
        /// </summary>
        internal static void UnhookApplyDefaultLevelToAllThrowExceptions() => AppDomain.CurrentDomain.FirstChanceException -= FirstThrown;
​
        static void FirstThrown(object sender, FirstChanceExceptionEventArgs e)
        {
            // We don't want to log TaskCanceledException / OperationCanceledException because they indicate control flow, rather than an error as such,
            // and don't want to log AggregateException because it would be essentially be a duplicate of the exception(s) that it wraps (which SHOULD
            // be logged)
            if ((e.Exception is OperationCanceledException) // Note: Also covers TaskCanceledException
            || (e.Exception is AggregateException))
                return;
​
            e.Exception.RecordLogLevel(ExceptionLogLevel.Critical, overrideAnyCurrentValue: false);
        }
​
        public static T Trace<T>(this T source, bool overrideAnyCurrentValue = true) where T : Exception =>
            source.RecordLogLevel(ExceptionLogLevel.Trace, overrideAnyCurrentValue);
​
        public static T Debug<T>(this T source, bool overrideAnyCurrentValue = true) where T : Exception =>
            source.RecordLogLevel(ExceptionLogLevel.Debug, overrideAnyCurrentValue);
​
        public static T Info<T>(this T source, bool overrideAnyCurrentValue = true) where T : Exception =>
            source.RecordLogLevel(ExceptionLogLevel.Info, overrideAnyCurrentValue);
​
        public static T Error<T>(this T source, bool overrideAnyCurrentValue = true) where T : Exception =>
            source.RecordLogLevel(ExceptionLogLevel.Error, overrideAnyCurrentValue);
​
        public static T Warning<T>(this T source, bool overrideAnyCurrentValue = true) where T : Exception =>
            source.RecordLogLevel(ExceptionLogLevel.Warning, overrideAnyCurrentValue);
​
        public static T Critical<T>(this T source, bool overrideAnyCurrentValue = true) where T : Exception =>
            source.RecordLogLevel(ExceptionLogLevel.Critical, overrideAnyCurrentValue);
​
        static T RecordLogLevel<T>(this T source, ExceptionLogLevel logLevel, bool overrideAnyCurrentValue) where T : Exception
        {
            if (overrideAnyCurrentValue || !source.Data.Contains(LogLevelKey))
                AddLogData(source, LogLevelKey, logLevel.ToString();
            return source;
        }
​
        // TODO: Maybe this is only required by unit tests if we're relying on Exceptional to record the log level value in OpServer?
        public static ExceptionLogLevel? TryToGetLogLevel(this Exception ex) =>
            // Note: ex.Data[..] will return null if the key is not found, so no need to call Contains before attempting access
            // TODO: If RecordLogLevel is changed to use Exceptional's AddLogData method then update this as well (Constants.CustomDataKeyPrefix + LogLevelKey)
            Enum.TryParse<ExceptionLogLevel>(ex.Data[Constants.CustomDataKeyPrefix + LogLevelKey] as string, out var result)
                ? result
                : (ExceptionLogLevel?)null;
    }
}
