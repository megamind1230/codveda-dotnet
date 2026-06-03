using Microsoft.AspNetCore.Builder;

namespace Kanban.Core.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app)
        => app.UseMiddleware<RequestTimingMiddleware>();
}
