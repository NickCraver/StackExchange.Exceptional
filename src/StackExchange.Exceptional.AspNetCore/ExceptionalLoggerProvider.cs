
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StackExchange.Exceptional
{
    class ExceptionalLoggerProvider : ILoggerProvider
    {
        private readonly IOptions<ExceptionalSettings> _settings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExceptionalLoggerProvider(IOptions<ExceptionalSettings> settings, IHttpContextAccessor httpContextAccessor = null)
        {
            _settings = settings;
            _httpContextAccessor = httpContextAccessor;
        }

        ILogger ILoggerProvider.CreateLogger(string categoryName)
            => new ExceptionalLogger(categoryName, _settings, _httpContextAccessor);

        void IDisposable.Dispose()
        {
        }
    }
}
