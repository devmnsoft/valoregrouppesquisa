namespace Valora.Api.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("HTTP {Method} {Path}", context.Request.Method, context.Request.Path);
        await next(context);
    }
}
