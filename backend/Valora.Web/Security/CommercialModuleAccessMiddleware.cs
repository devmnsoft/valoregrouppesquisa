using System.Security.Claims;
using System.Text.Json;
using Valora.Application.ModularSaas;

namespace Valora.Web.Security;

public sealed class CommercialModuleAccessMiddleware(RequestDelegate next, ILogger<CommercialModuleAccessMiddleware> logger) {
    private static readonly IReadOnlyList<(PathString Prefix, string Module)> Routes =
    [
        ("/Diagnostics", "diagnostics"), ("/Forms", "forms"), ("/Surveys", "surveys"),
        ("/Responses", "surveys"), ("/Results", "results"), ("/Reports", "reports"),
        ("/Certificates", "certificates"), ("/ActionCenter", "action_center"), ("/ActionPlans", "action_center"),
        ("/ValoraAction", "action_center"), ("/Evolution", "evolution"), ("/Journey", "journey"),
        ("/Indicators", "indicators"), ("/Benchmarks", "benchmarks"), ("/Methodology", "methodology_studio"),
        ("/Insights", "valora_ai"), ("/DataHub", "data_hub"), ("/Intelligence/Integrations", "data_hub"), ("/Governance", "governance"),
        ("/DecisionCenter", "governance"), ("/Decisions", "governance"),
        ("/SecurityCompliance", "security_compliance"), ("/SuccessCenter", "success_center")
    ];

    public async Task InvokeAsync(HttpContext context, CommercialSaasService saas) {
        if (context.User.Identity?.IsAuthenticated != true || IsPlatformAdministrator(context.User)) {
            await next(context);
            return;
        }

        var route = Routes.FirstOrDefault(candidate => context.Request.Path.StartsWithSegments(candidate.Prefix));
        if (route == default) {
            await next(context);
            return;
        }

        if (!Guid.TryParse(context.User.FindFirstValue("organization_id"), out var clientId) || clientId == Guid.Empty) {
            await DenyAsync(context, route.Module, "CLIENT_CONTEXT_REQUIRED", "Selecione um cliente para operar esta área.");
            return;
        }

        try {
            var writeOperation = !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method);
            var decision = await saas.EvaluateAccessAsync(clientId, route.Module, writeOperation, context.RequestAborted);
            if (!decision.Allowed) {
                logger.LogWarning(
                    "Commercial module access denied. UserId={UserId} ClientId={ClientId} ModuleCode={ModuleCode} Action={Action} Status={Status} CorrelationId={CorrelationId}",
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier), clientId, route.Module,
                    context.Request.Method, decision.Code, context.TraceIdentifier);
                await DenyAsync(context, route.Module, decision.Code, decision.Message);
                return;
            }
            if (decision.ReadOnly) context.Response.Headers["X-Valora-Module-Access"] = "read-only";
            await next(context);
        }
        catch (Exception exception) {
            logger.LogError(exception,
                "Commercial module authorization failed. UserId={UserId} ClientId={ClientId} ModuleCode={ModuleCode} CorrelationId={CorrelationId}",
                context.User.FindFirstValue(ClaimTypes.NameIdentifier), clientId, route.Module, context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new {
                status = 503,
                code = "MODULE_ACCESS_UNAVAILABLE",
                message = "Não foi possível validar o acesso agora. Tente novamente.",
                correlationId = context.TraceIdentifier
            }));
        }
    }

    private static bool IsPlatformAdministrator(ClaimsPrincipal user) =>
        user.IsInRole("admin_valora") || user.IsInRole("SuperAdmin") || user.IsInRole("platform_admin");

    private static Task DenyAsync(HttpContext context, string moduleCode, string code, string message) {
        if (HttpMethods.IsGet(context.Request.Method) && !context.Request.Path.StartsWithSegments("/bff")) {
            context.Response.Redirect($"/Modules?blocked={Uri.EscapeDataString(moduleCode)}&reason={Uri.EscapeDataString(code)}");
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new {
            status = 403,
            code,
            message,
            correlationId = context.TraceIdentifier
        }));
    }
}
