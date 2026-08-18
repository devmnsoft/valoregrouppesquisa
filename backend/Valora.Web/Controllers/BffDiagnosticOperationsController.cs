using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/diagnostics/{id:guid}")]
public sealed class BffDiagnosticOperationsController(IBffApiClient api, BffAuthenticationService authentication) : ControllerBase
{
    [HttpGet("participation")]
    public Task<IActionResult> Participation(Guid id, CancellationToken ct) => Forward(id, "participation", ct);

    [AcceptVerbs("GET", "POST"), Route("campaign/{action?}")]
    public Task<IActionResult> Campaign(Guid id, string? action, CancellationToken ct) =>
        Forward(id, string.IsNullOrWhiteSpace(action) ? "campaign" : $"campaign/{action}", ct);

    [AcceptVerbs("GET", "POST"), Route("executive-report/{action?}")]
    public Task<IActionResult> ExecutiveReport(Guid id, string? action, CancellationToken ct) =>
        Forward(id, string.IsNullOrWhiteSpace(action) ? "executive-report" : $"executive-report/{action}", ct);

    private async Task<IActionResult> Forward(Guid id, string resource, CancellationToken ct)
    {
        var session = await authentication.GetAsync(HttpContext, ct);
        if (session is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = HttpContext.TraceIdentifier });

        object? body = null;
        if (Request.ContentLength > 0)
            body = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body, cancellationToken: ct);

        var correlation = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        using var response = await api.SendAsync(new HttpMethod(Request.Method), $"/api/v1/diagnostics/{id}/{resource}{Request.QueryString}", body, session.AccessToken, correlation, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.First() : correlation;
        return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", Content = payload };
    }
}
