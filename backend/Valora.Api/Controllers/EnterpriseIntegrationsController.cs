using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts.Services;
using Valora.Application.Enterprise;

namespace Valora.Api.Controllers;

[Authorize, ApiController]
public sealed class EnterpriseIntegrationsController(EnterpriseService service, IEntitlementService entitlements) : ControllerBase
{
    private const string LockedMessage = "Este recurso faz parte dos módulos Enterprise do Valora Insight™.";
    private Guid? OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : null;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet("api/v1/integrations")]
    public async Task<IActionResult> Integrations(CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!await IsEnterprise(organizationId)) return EnterpriseLocked();
        var saved = await service.ItemsAsync(organizationId, "integration", ct);
        string[] catalog = ["public-api", "webhooks", "powerbi", "exports", "smtp", "certificates-pdf", "assisted-import"];
        return Ok(catalog.Select(code => saved.FirstOrDefault(x => ConfigCode(x.Configuration) == code) is { } item
            ? new { code, name = item.Name, status = item.Status, configured = item.Status == "configured", lastExecutionAt = (DateTime?)null, requiredPlan = "enterprise" }
            : new { code, name = DisplayName(code), status = "not_configured", configured = false, lastExecutionAt = (DateTime?)null, requiredPlan = "enterprise" }));
    }

    [HttpGet("api/v1/integrations/{code}")]
    public async Task<IActionResult> Integration(string code, CancellationToken ct)
    {
        var result = await Integrations(ct) as OkObjectResult;
        if (result?.Value is not IEnumerable<object> rows) return result ?? EnterpriseLocked();
        return rows.FirstOrDefault(x => string.Equals(x.GetType().GetProperty("code")?.GetValue(x)?.ToString(), code, StringComparison.OrdinalIgnoreCase)) is { } row ? Ok(row) : NotFound();
    }

    [HttpPatch("api/v1/integrations/{code}")]
    public async Task<IActionResult> Configure(string code, [FromBody] IntegrationRequest request, CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!await IsEnterprise(organizationId)) return EnterpriseLocked();
        if (request.Configuration.ValueKind != JsonValueKind.Object) return BadRequest(new { message = "Informe uma configuração válida." });
        var id = await service.SaveItemAsync(organizationId, request.Id, new("integration", DisplayName(code), request.Enabled ? "configured" : "disabled", request.Configuration), UserId, ct);
        return Ok(new { id, code, status = request.Enabled ? "configured" : "disabled" });
    }

    [HttpGet("api/v1/api-keys")]
    public async Task<IActionResult> ApiKeys(CancellationToken ct) => await Guard(ct, o => service.ApiKeysAsync(o, ct));

    [HttpPost("api/v1/api-keys")]
    public async Task<IActionResult> CreateApiKey([FromBody] ApiKeyRequest request, CancellationToken ct) =>
        await Guard(ct, o => service.CreateApiKeyAsync(o, request.Name, request.Scopes, UserId, ct));

    [HttpPost("api/v1/api-keys/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!await IsEnterprise(organizationId)) return EnterpriseLocked();
        await service.RevokeApiKeyAsync(organizationId, id, UserId, ct);
        return NoContent();
    }

    [HttpPost("api/v1/api-keys/{id:guid}/rotate")]
    public async Task<IActionResult> RotateApiKey(Guid id, [FromBody] ApiKeyRequest request, CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!await IsEnterprise(organizationId)) return EnterpriseLocked();
        await service.RevokeApiKeyAsync(organizationId, id, UserId, ct);
        return Ok(await service.CreateApiKeyAsync(organizationId, request.Name, request.Scopes, UserId, ct));
    }

    private async Task<IActionResult> Guard<T>(CancellationToken ct, Func<Guid, Task<T>> action)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!await IsEnterprise(organizationId)) return EnterpriseLocked();
        return Ok(await action(organizationId));
    }
    private Task<bool> IsEnterprise(Guid organizationId) => entitlements.CanUseAsync(organizationId, "enterprise_integrations");
    private ObjectResult EnterpriseLocked() => StatusCode(403, new { code = "ENTERPRISE_MODULE_REQUIRED", message = LockedMessage });
    private static string? ConfigCode(JsonElement value) => value.TryGetProperty("code", out var code) ? code.GetString() : null;
    private static string DisplayName(string code) => code switch { "public-api" => "API Pública", "webhooks" => "Webhooks", "powerbi" => "Power BI Prepared Dataset", "exports" => "Exportações BI", "smtp" => "E-mail / SMTP", "certificates-pdf" => "Certificados / PDF", "assisted-import" => "Importação Assistida", _ => code };
    public sealed record IntegrationRequest(Guid? Id, bool Enabled, JsonElement Configuration);
    public sealed record ApiKeyRequest(string Name, IReadOnlyList<string> Scopes);
}
