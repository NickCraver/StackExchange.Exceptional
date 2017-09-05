using StackExchange.Exceptional;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the Exceptional middleware.
    /// </summary>
    public static class ExceptionalBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for capturing exceptions that occur during HTTP requests.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
        public static IApplicationBuilder UseExceptional(this IApplicationBuilder builder)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            return builder.UseMiddleware<ExceptionalMiddleware>();
        }
    }
}