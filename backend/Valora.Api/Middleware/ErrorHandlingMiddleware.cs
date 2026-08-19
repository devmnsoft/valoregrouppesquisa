using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using Valora.Application.Exceptions;
using Valora.Application.Security;

namespace Valora.Api.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, code, message) = MapException(ex);
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var value) ? value?.ToString() : Guid.NewGuid().ToString("N");
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var safePath = SanitizePathAndQuery(context.Request.Path.Value, context.Request.QueryString.Value);

        if (ex is PostgresException postgresException)
            logger.LogError(ex, "PostgreSQL failure. SqlState={SqlState} TableName={TableName} ColumnName={ColumnName} ConstraintName={ConstraintName} CorrelationId={CorrelationId}", postgresException.SqlState, postgresException.TableName, postgresException.ColumnName, postgresException.ConstraintName, correlationId);
        else if (status >= 500)
            logger.LogError(ex, "API exception. StatusCode={StatusCode} ErrorCode={ErrorCode} TraceId={TraceId} CorrelationId={CorrelationId} Path={Path}", status, code, traceId, correlationId, safePath);
        else
            logger.LogWarning(ex, "Expected API exception. StatusCode={StatusCode} ErrorCode={ErrorCode} TraceId={TraceId} CorrelationId={CorrelationId} Path={Path}", status, code, traceId, correlationId, safePath);

        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.io/{status}",
            ["status"] = status,
            ["code"] = code,
            ["title"] = TitleFor(code, status),
            ["detail"] = message,
            ["correlationId"] = correlationId,
            ["fieldErrors"] = new Dictionary<string, string[]>(),
            ["suggestedAction"] = SuggestedActionFor(status),
            // Transitional fields retained for existing BFF clients.
            ["ok"] = false,
            ["message"] = message,
            ["traceId"] = traceId
        };
        if (environment.IsDevelopment()) payload["exceptionType"] = ex.GetType().Name;
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    private static string TitleFor(string code, int status) => code == "DATABASE_SCHEMA_MISMATCH"
        ? "Falha de configuração do ambiente"
        : code == "APPLICATION_CONFIGURATION_ERROR" ? "Falha de configuração da aplicação"
        : status switch
    {
        400 => "Revise os dados informados",
        401 => "Sua sessão precisa ser renovada",
        403 => "Você não possui acesso a esta ação",
        404 => "Não encontramos este recurso",
        409 => "O registro foi atualizado",
        422 => "Não foi possível concluir a operação",
        503 => "Serviço temporariamente indisponível",
        504 => "A operação levou mais tempo que o esperado",
        _ => "Ocorreu um erro inesperado"
    };

    private static string SuggestedActionFor(int status) => status switch
    {
        400 or 422 => "Revise os campos destacados e tente novamente.",
        401 => "Entre novamente para continuar.",
        403 => "Solicite a permissão necessária ao administrador da organização.",
        404 => "Volte à listagem e confirme se o registro ainda existe.",
        409 => "Atualize a página antes de repetir a alteração.",
        503 or 504 => "Aguarde alguns instantes e tente novamente.",
        _ => "Tente novamente. Se o problema continuar, informe o código de correlação ao suporte."
    };

    private static string SanitizePathAndQuery(string? path, string? query)
    {
        var value = string.Concat(path ?? string.Empty, query ?? string.Empty);
        value = Regex.Replace(value, "(?i)(token|resultToken|publicToken|result_token_hash|token_hash)=([^&]+)", "$1=***");
        return LogSanitizer.MaskConnectionString(value) ?? string.Empty;
    }

    private static (int Status, string Code, string Message) MapException(Exception ex) => ex switch
    {
        ValidationAppException => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Requisição inválida."),
        ArgumentNullException => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Erro interno. Tente novamente ou acione o suporte."),
        ArgumentException => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Requisição inválida."),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "AUTH_INVALID_CREDENTIALS", "Não foi possível autenticar. Verifique suas credenciais e tente novamente."),
        InactiveUserException inactive => (StatusCodes.Status403Forbidden, "AUTH_USER_INACTIVE", inactive.Message),
        OrganizationAccessNotConfiguredException access => (StatusCodes.Status403Forbidden, "AUTH_ACCESS_NOT_CONFIGURED", access.Message),
        ApplicationConfigurationException => (StatusCodes.Status500InternalServerError, "APPLICATION_CONFIGURATION_ERROR", "A configuração da aplicação está incompleta."),
        ForbiddenAppException => (StatusCodes.Status403Forbidden, "FORBIDDEN", "Acesso proibido."),
        NotFoundAppException or KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", "Recurso não encontrado."),
        ConflictAppException or ConcurrencyConflictException => (StatusCodes.Status409Conflict, "CONCURRENCY_CONFLICT", "O recurso foi alterado por outra sessão."),
        BusinessRuleAppException business when business.Message.StartsWith("CAPABILITY_NOT_AVAILABLE:", StringComparison.Ordinal) => (StatusCodes.Status422UnprocessableEntity, "CAPABILITY_NOT_AVAILABLE", "O plano contratado não disponibiliza este recurso."),
        BusinessRuleAppException business when business.Message.StartsWith("LAST_ADMINISTRATOR:", StringComparison.Ordinal) => (StatusCodes.Status422UnprocessableEntity, "LAST_ADMINISTRATOR", "O último administrador não pode ser desativado."),
        BusinessRuleAppException business when business.Message.StartsWith("LAST_ADMIN_ROLE:", StringComparison.Ordinal) => (StatusCodes.Status422UnprocessableEntity, "LAST_ADMIN_ROLE", "A última role administrativa não pode ser removida."),
        BusinessRuleAppException or InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, "BUSINESS_RULE_ERROR", "Não foi possível concluir a operação."),
        PostgresException postgres when postgres.SqlState is "42703" or "42P01" or "42883" =>
            (StatusCodes.Status500InternalServerError, "DATABASE_SCHEMA_MISMATCH", "A estrutura de dados da aplicação precisa ser atualizada."),
        PostgresException postgres when postgres.SqlState.StartsWith("08", StringComparison.Ordinal) =>
            (StatusCodes.Status503ServiceUnavailable, "DATABASE_UNAVAILABLE", "Banco de dados temporariamente indisponível."),
        NpgsqlException => (StatusCodes.Status503ServiceUnavailable, "DATABASE_UNAVAILABLE", "Banco de dados temporariamente indisponível."),
        TimeoutException => (StatusCodes.Status504GatewayTimeout, "TIMEOUT", "Tempo limite excedido."),
        HttpRequestException => (StatusCodes.Status502BadGateway, "EXTERNAL_SERVICE_ERROR", "Falha em integração externa."),
        _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Erro interno. Tente novamente ou acione o suporte.")
    };
}
