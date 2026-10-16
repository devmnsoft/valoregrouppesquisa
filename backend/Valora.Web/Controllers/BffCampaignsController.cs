using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize, ApiController, AutoValidateAntiforgeryToken, Route("bff/campaigns")]
public sealed class BffCampaignsController(
    IBffApiClient api,
    BffAuthenticationService authentication) : ControllerBase {

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) =>
        ForwardPath($"/api/v1/campaigns{Request.QueryString}", false, ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        ForwardPath($"/api/v1/campaigns/{id}", false, ct);

    [HttpPost]
    public Task<IActionResult> Create(CancellationToken ct) =>
        ForwardPath("/api/v1/campaigns", true, ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, CancellationToken ct) =>
        ForwardPath($"/api/v1/campaigns/{id}", true, ct);

    [HttpPost("{id:guid}/{actionName}")]
    public Task<IActionResult> Transition(Guid id, string actionName, CancellationToken ct) =>
        ForwardPath($"/api/v1/campaigns/{id}/{actionName}", false, ct);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ForwardPath($"/api/v1/campaigns/{id}", false, ct);

    private async Task<IActionResult> ForwardPath(string path, bool isBodyRequired, CancellationToken ct) {
        var session = await authentication.GetAsync(HttpContext, ct);
        if (session is null)
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Sua sessão expirou. Entre novamente.", correlationId = HttpContext.TraceIdentifier });

        var correlation = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (success, body, error) = await BffBodyReader.ReadJsonBodyAsync(Request, correlation, isBodyRequired: isBodyRequired, cancellationToken: ct);
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
        return new ContentResult {
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            Content = payload
        };
    }
}
