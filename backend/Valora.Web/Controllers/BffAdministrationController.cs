using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[ApiController]
[Route("bff")]
public sealed class BffAdministrationController(IBffApiClient api, BffAuthenticationService authentication) : ControllerBase
{
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("organization/{**resource}")]
    public Task<IActionResult> Organization(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/api/v1/organization/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("users/{**resource}")]
    public Task<IActionResult> Users(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/api/v1/users/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("access/{**resource}")]
    public Task<IActionResult> Access(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/access/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("roles/{**resource}")]
    public Task<IActionResult> Roles(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/roles/{resource}", cancellationToken);

    [HttpGet("permissions")]
    public Task<IActionResult> Permissions(CancellationToken cancellationToken) => ForwardAsync("/api/v1/permissions", cancellationToken);

    [HttpGet("plans/{**resource}")]
    public Task<IActionResult> Plans(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/plans/{resource}", cancellationToken);

    [HttpGet("audit")]
    public Task<IActionResult> Audit(CancellationToken cancellationToken) => ForwardAsync("/api/v1/audit", cancellationToken);

    [AcceptVerbs("GET", "POST")]
    [Route("notifications/{**resource}")]
    public Task<IActionResult> Notifications(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/notifications/{resource}", cancellationToken);

    [HttpGet("platform-governance/{**resource}")]
    public Task<IActionResult> Governance(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/platform-governance/{resource}", cancellationToken);

    [HttpGet("system-health")]
    public Task<IActionResult> SystemHealth(CancellationToken cancellationToken) => ForwardAsync("/api/v1/system-health", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("business-groups/{**resource}")]
    public Task<IActionResult> BusinessGroups(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/business-groups/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("legal-entities/{**resource}")]
    public Task<IActionResult> LegalEntities(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/legal-entities/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("units/{**resource}")]
    public Task<IActionResult> Units(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/units/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("departments/{**resource}")]
    public Task<IActionResult> Departments(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/departments/{resource}", cancellationToken);

    [HttpGet("dashboard/executive-summary")]
    public Task<IActionResult> Dashboard(CancellationToken cancellationToken) =>
        ForwardAsync("/api/v1/dashboard/executive-summary", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("enterprise/{**resource}")]
    public Task<IActionResult> Enterprise(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/api/v1/enterprise/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("integrations/{**resource}")]
    public Task<IActionResult> Integrations(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/integrations/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("api-keys/{**resource}")]
    public Task<IActionResult> ApiKeys(string? resource, CancellationToken cancellationToken) => ForwardAsync($"/api/v1/api-keys/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("forms/{**resource}")]
    public Task<IActionResult> Forms(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/api/v1/forms/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("surveys/{**resource}")]
    public Task<IActionResult> Surveys(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/surveys/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("survey-links/{**resource}")]
    public Task<IActionResult> SurveyLinks(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/survey-links/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST", "PATCH")]
    [Route("experience/{**resource}")]
    public Task<IActionResult> Experience(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/api/v1/experience/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST")]
    [Route("methodology/{**resource}")]
    public Task<IActionResult> Methodology(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/api/v1/methodology/{resource}", cancellationToken);

    [AcceptVerbs("GET", "POST")]
    [Route("reports/{**resource}")]
    public Task<IActionResult> Reports(string? resource, CancellationToken cancellationToken) =>
        ForwardAsync($"/reports/{resource}", cancellationToken);

    private async Task<IActionResult> ForwardAsync(string path, CancellationToken cancellationToken)
    {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        if (session is null) return Unauthorized(new { message = "Sessão expirada." });
        object? body = null;
        if (Request.ContentLength > 0)
            body = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body, cancellationToken: cancellationToken);
        var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        using var response = await api.SendAsync(new HttpMethod(Request.Method), path + query, body, session.AccessToken, correlationId, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        Response.Headers["X-Correlation-Id"] = response.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.First() : correlationId;
        return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json", Content = payload };
    }
}
