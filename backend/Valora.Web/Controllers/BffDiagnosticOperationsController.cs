using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;
using Valora.Application.Access;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/diagnostics/{id:guid}")]
public sealed class BffDiagnosticOperationsController(IBffApiClient api, BffAuthenticationService authentication, IAuthorizationService authorization) : ControllerBase {
    [HttpGet("/bff/diagnostic-campaigns")]
    public Task<IActionResult> Campaigns(CancellationToken ct) => ForwardPath("/api/v1/diagnostic-campaigns", ct);

    [HttpGet("participation")]
    public Task<IActionResult> Participation(Guid id, CancellationToken ct) => Forward(id, "participation", ct);

    [AcceptVerbs("GET", "POST"), Route("campaign/{command?}")]
    public async Task<IActionResult> Campaign(Guid id, string? command, CancellationToken ct) {
        if (HttpMethods.IsGet(Request.Method) && string.IsNullOrWhiteSpace(command)) {
            Response.Headers["X-Valora-Campaign-Manage"] = (await authorization.AuthorizeAsync(User, null, ValoraPermissions.Campaigns.Manage)).Succeeded ? "true" : "false";
            Response.Headers["X-Valora-Campaign-Distribute"] = (await authorization.AuthorizeAsync(User, null, ValoraPermissions.Surveys.Distribute)).Succeeded ? "true" : "false";
        }
        return await Forward(id, string.IsNullOrWhiteSpace(command) ? "campaign" : $"campaign/{command}", ct);
    }

    [AcceptVerbs("GET", "POST"), Route("executive-report/{command?}")]
    public Task<IActionResult> ExecutiveReport(Guid id, string? command, CancellationToken ct) =>
        Forward(id, string.IsNullOrWhiteSpace(command) ? "executive-report" : $"executive-report/{command}", ct);

    private async Task<IActionResult> Forward(Guid id, string resource, CancellationToken ct)
        => await ForwardPath($"/api/v1/diagnostics/{id}/{resource}{Request.QueryString}", ct);

    private async Task<IActionResult> ForwardPath(string path, CancellationToken ct) {
        var session = await authentication.GetAsync(HttpContext, ct);
        if (session is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = HttpContext.TraceIdentifier });

        var correlation = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (success, body, error) = await BffBodyReader.ReadJsonBodyAsync(Request, correlation, isBodyRequired: false, cancellationToken: ct);
        if (!success) return error!;

        using var response = await api.SendAsync(new HttpMethod(Request.Method), path, body, session.AccessToken, correlation, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
            Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var v) ? v.First() : correlation;
            return NoContent();
        }
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (response.Headers.TryGetValues("X-Valora-Module-Access", out var moduleAccess))
            Response.Headers["X-Valora-Module-Access"] = moduleAccess.First();
        Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.First() : correlation;
        return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", Content = payload };
    }
}
