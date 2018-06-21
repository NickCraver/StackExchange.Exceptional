using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StackExchange.Exceptional
{
    class ExceptionalLogger : ILogger
    {
        private readonly string _category;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<ExceptionalSettings> _settings;
        private readonly bool _ignored;
        private static readonly HashSet<string> _ignoredCategories = new HashSet<string>
        {
            typeof(ExceptionalMiddleware).FullName, // exceptional middleware calls some ILogger stuff itself, ignore those calls)
        };

        public ExceptionalLogger(string category, IOptions<ExceptionalSettings> settings, IHttpContextAccessor httpContextAccessor = null)
        {
            _category = category;
            _settings = settings;
            _httpContextAccessor = httpContextAccessor;
            _ignored = _ignoredCategories.Contains(category);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return !_ignored;
            // TODO compare against settings and add support for per-category log levels, or we can just leave it up to LogFilters to decide later
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (_ignored) return;
            if (exception == null) return;

            var customData = new Dictionary<string, string>
            {
                ["AspNetCore.LogLevel"] = logLevel + "",
                ["AspNetCore.EventId.Id"] = eventId.Id + "",
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
