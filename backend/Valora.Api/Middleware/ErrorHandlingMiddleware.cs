using System.Diagnostics;
using System.Text.Json;
using Valora.Application.Exceptions;

namespace Valora.Api.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, code, message) = MapException(ex);
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var value)
            ? value?.ToString()
            : Guid.NewGuid().ToString("N");
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        logger.LogError(ex,
            "Unhandled API exception. StatusCode={StatusCode} ErrorCode={ErrorCode} TraceId={TraceId} CorrelationId={CorrelationId} Path={Path}",
            status, code, traceId, correlationId, context.Request.Path.Value);

        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        }

        var payload = new Dictionary<string, object?>
        {
            ["ok"] = false,
            ["message"] = message,
            ["code"] = code,
            ["traceId"] = traceId,
            ["correlationId"] = correlationId
        };

        if (environment.IsDevelopment())
        {
            payload["exceptionType"] = ex.GetType().Name;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    private static (int Status, string Code, string Message) MapException(Exception ex) => ex switch
    {
        ValidationAppException or ArgumentException => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Requisição inválida."),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Acesso não autorizado."),
        ForbiddenAppException => (StatusCodes.Status403Forbidden, "FORBIDDEN", "Acesso proibido."),
        NotFoundAppException or KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", "Recurso não encontrado."),
        ConflictAppException => (StatusCodes.Status409Conflict, "CONFLICT", "Conflito na operação."),
        BusinessRuleAppException or InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, "BUSINESS_RULE_ERROR", "Não foi possível concluir a operação."),
        _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Erro interno. Tente novamente ou acione o suporte.")
    };
}
