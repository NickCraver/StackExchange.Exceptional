using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StackExchange.Exceptional
{
    internal class ExceptionalLogger : ILogger
    {
        private readonly string _category;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<ExceptionalSettings> _settings;

        public ExceptionalLogger(string category, IOptions<ExceptionalSettings> settings, IHttpContextAccessor httpContextAccessor = null)
        {
            _category = category;
            _settings = settings;
            _httpContextAccessor = httpContextAccessor;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => _settings.Value.ILoggerLevel <= logLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Ignore non-exceptions and Exceptional events themselves
            if (exception == null || (ExceptionalLoggingEvents.Min <=eventId.Id && eventId.Id <= ExceptionalLoggingEvents.Max))
            {
                return;
            }

            var customData = new Dictionary<string, string>
            {
                ["AspNetCore.LogLevel"] = logLevel.ToString(),
                ["AspNetCore.EventId.Id"] = eventId.Id.ToString(),
                ["AspNetCore.EventId.Name"] = eventId.Name,
                ["AspNetCore.Message"] = formatter(state, exception),
            };

            if (_httpContextAccessor?.HttpContext is HttpContext context)
            {
                exception.Log(context, _category, customData: customData);
            }
            else
            {
                exception.LogNoContext(_category, customData: customData);
            }
        }
    }
}
