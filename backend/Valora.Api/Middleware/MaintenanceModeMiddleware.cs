using System.Security.Claims;

namespace Valora.Api.Middleware;

public sealed class MaintenanceModeMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string DefaultMessage = "O sistema está em manutenção programada. Tente novamente mais tarde.";

    public async Task InvokeAsync(HttpContext context)
    {
        var enabled = configuration.GetValue<bool>("App:MaintenanceModeEnabled");
        var isWrite = !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method);
        var isPublicResponse = context.Request.Path.StartsWithSegments("/api/v1/public") || context.Request.Path.StartsWithSegments("/api/public");
        var blockPublic = configuration.GetValue("App:MaintenanceBlockPublicResponses", true);
        var blockAdmin = configuration.GetValue("App:MaintenanceBlockAdminWrites", true);
        var isPlatformAdmin = context.User.IsInRole("platform_admin") || context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "platform_admin");
        var operationalRoute = context.Request.Path.StartsWithSegments("/health") || context.Request.Path.StartsWithSegments("/swagger");
        var shouldBlock = isWrite && (isPublicResponse ? blockPublic : blockAdmin);
        if (!enabled || !shouldBlock || isPlatformAdmin || operationalRoute)
        {
            await next(context);
            return;
        }

        var correlationId = context.Items[CorrelationIdMiddleware.ItemName]?.ToString() ?? context.TraceIdentifier;
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.io/503", status = 503, code = "MAINTENANCE_MODE",
            title = "Manutenção programada", detail = configuration["App:MaintenanceMessage"] ?? DefaultMessage, correlationId
        });
    }
}
