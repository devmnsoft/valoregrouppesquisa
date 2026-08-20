using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
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
        string[] catalog = ["public-api", "api-keys", "webhooks", "powerbi", "exports", "cnpj-cep", "smtp", "assisted-import", "integration-logs", "certificates-pdf", "executive-report-pdf"];
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
        if (!KnownIntegration(code)) return NotFound();
        if (request.Configuration.ValueKind != JsonValueKind.Object) return BadRequest(Error("INVALID_CONFIGURATION", "Informe uma configuração válida."));
        if (ContainsSecret(request.Configuration)) return BadRequest(Error("SENSITIVE_CONFIGURATION", "Tokens, chaves e secrets devem ser configurados no cofre seguro do ambiente."));
        var configuration = JsonSerializer.SerializeToElement(new { code, settings = request.Configuration });
        var id = await service.SaveItemAsync(organizationId, request.Id, new("integration", DisplayName(code), request.Enabled ? "configured" : "disabled", configuration), UserId, ct);
        return Ok(new { id, code, status = request.Enabled ? "configured" : "disabled" });
    }

    [HttpPost("api/v1/integrations/{code}/disable")]
    public async Task<IActionResult> Disable(string code, CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!await IsEnterprise(organizationId)) return EnterpriseLocked();
        var item = (await service.ItemsAsync(organizationId, "integration", ct)).FirstOrDefault(x => ConfigCode(x.Configuration) == code);
        if (item is null) return NotFound(Error("INTEGRATION_NOT_FOUND", "Esta integração ainda não está configurada neste ambiente."));
        await service.SaveItemAsync(organizationId, item.Id, new("integration", item.Name, "disabled", item.Configuration), UserId, ct);
        return NoContent();
    }

    [HttpPost("api/v1/integrations/{code}/test")]
    public async Task<IActionResult> Test(string code, CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!await IsEnterprise(organizationId)) return EnterpriseLocked();
        var item = (await service.ItemsAsync(organizationId, "integration", ct)).FirstOrDefault(x => ConfigCode(x.Configuration) == code && x.Status == "configured");
        if (item is null) return Conflict(Error("INTEGRATION_NOT_CONFIGURED", "Esta integração ainda não está configurada neste ambiente."));
        return StatusCode(501, Error("CONNECTION_TEST_UNAVAILABLE", "O teste real deste conector não está disponível neste ambiente; nenhuma conexão foi simulada."));
    }

    [HttpGet("api/v1/api-keys")]
    public async Task<IActionResult> ApiKeys(CancellationToken ct) => await Guard(ct, o => service.ApiKeysAsync(o, ct));

    [HttpPost("api/v1/api-keys")]
    public async Task<IActionResult> CreateApiKey([FromBody] ApiKeyRequest request, CancellationToken ct) =>
        await Guard(ct, o => service.CreateApiKeyAsync(o, request.Name, request.Scopes, request.ExpiresAt, UserId, ct));

    [HttpGet("api/v1/api-keys/{id:guid}")]
    public async Task<IActionResult> ApiKey(Guid id, CancellationToken ct) => await Guard(ct, o => service.ApiKeyAsync(o, id, ct));

    [HttpGet("api/v1/api-keys/{id:guid}/usage")]
    public async Task<IActionResult> ApiKeyUsage(Guid id, CancellationToken ct) => await Guard(ct, o => service.ApiKeyUsageAsync(o, id, ct));

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
        return Ok(await service.CreateApiKeyAsync(organizationId, request.Name, request.Scopes, request.ExpiresAt, UserId, ct));
    }

    private async Task<IActionResult> Guard<T>(CancellationToken ct, Func<Guid, Task<T>> action)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!await IsEnterprise(organizationId)) return EnterpriseLocked();
        return Ok(await action(organizationId));
    }
    private Task<bool> IsEnterprise(Guid organizationId) => entitlements.CanUseAsync(organizationId, FeatureCodes.EnterpriseIntegrations);
    private ObjectResult EnterpriseLocked() => StatusCode(403, Error("ENTERPRISE_MODULE_REQUIRED", LockedMessage));
    private object Error(string code, string message) => new { code, message, correlationId = HttpContext.TraceIdentifier };
    private static string? ConfigCode(JsonElement value) => value.TryGetProperty("code", out var code) ? code.GetString() : null;
    private static bool KnownIntegration(string code) => code is "public-api" or "api-keys" or "webhooks" or "powerbi" or "exports" or "cnpj-cep" or "smtp" or "integration-logs" or "certificates-pdf" or "executive-report-pdf" or "assisted-import";
    private static bool ContainsSecret(JsonElement value) => value.EnumerateObject().Any(x => x.Name.Contains("secret", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("token", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("password", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("apiKey", StringComparison.OrdinalIgnoreCase));
    private static string DisplayName(string code) => code switch { "public-api" => "API Pública", "api-keys" => "API Keys", "webhooks" => "Webhooks", "powerbi" => "Power BI Prepared Dataset", "exports" => "Exportações BI", "cnpj-cep" => "CNPJ / CEP", "smtp" => "E-mail transacional", "integration-logs" => "Logs e auditoria", "certificates-pdf" => "Certificados / PDF", "executive-report-pdf" => "Executive Report / PDF", "assisted-import" => "Importação Assistida", _ => code };
    public sealed record IntegrationRequest(Guid? Id, bool Enabled, JsonElement Configuration);
    public sealed record ApiKeyRequest(string Name, IReadOnlyList<string> Scopes, DateTime? ExpiresAt);
}
