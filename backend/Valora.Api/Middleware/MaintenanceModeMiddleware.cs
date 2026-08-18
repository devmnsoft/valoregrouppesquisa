using System.Security.Claims;

namespace Valora.Api.Middleware;

public sealed class MaintenanceModeMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string DefaultMessage = "O sistema está em manutenção programada. Tente novamente mais tarde.";

    public async Task InvokeAsync(HttpContext context)
    {
        var enabled = configuration.GetValue<bool>("App:MaintenanceModeEnabled");
        var isWrite = !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method);
        var isPlatformAdmin = context.User.IsInRole("platform_admin") || context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "platform_admin");
        var operationalRoute = context.Request.Path.StartsWithSegments("/health") || context.Request.Path.StartsWithSegments("/swagger");
        if (!enabled || !isWrite || isPlatformAdmin || operationalRoute)
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
