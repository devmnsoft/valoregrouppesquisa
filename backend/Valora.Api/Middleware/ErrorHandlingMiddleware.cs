namespace Valora.Api.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context) => next(context);
}
