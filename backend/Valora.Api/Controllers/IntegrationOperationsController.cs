using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Enterprise;
using Valora.Application.Integrations;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/integration-operations")]
public sealed class IntegrationOperationsController(EnterpriseService enterprise, IIntegrationRepository repository, ICnpjLookupService cnpj, ICepLookupService cep, ExternalImportValidator imports) : ControllerBase
{
    private Guid? OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : null;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet("cnpj/{value}")]
    public async Task<IActionResult> Cnpj(string value, CancellationToken ct) => Ok(await cnpj.LookupAsync(Digits(value), ct));
    [HttpGet("cep/{value}")]
    public async Task<IActionResult> Cep(string value, CancellationToken ct) => Ok(await cep.LookupAsync(Digits(value), ct));

    [HttpPost("imports/csv")]
    public async Task<IActionResult> Import([FromBody] ImportRequest request, CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        var rows = imports.ValidateCsv(request.Type, request.Content);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Content))).ToLowerInvariant();
        var id = await repository.CreateImportAsync(organizationId, request.Type, "csv", checksum, rows, ct);
        return Ok(new { id, status = rows.Any(x => x.Errors.Count != 0) ? "invalid" : "validated", rows });
    }

    [HttpPost("email")]
    public async Task<IActionResult> Email([FromBody] EmailRequest request, CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        string[] templates = ["diagnosis.invitation", "diagnosis.reminder", "report.available", "certificate.issued", "plan.limit_reached", "invoice.generated", "security.password"];
        if (!templates.Contains(request.Template)) return BadRequest(new { message = "Template transacional inválido." });
        return Accepted(new { id = await repository.EnqueueEmailAsync(organizationId, request.Template, request.Recipient, request.Payload, ct), status = "pending" });
    }

    [HttpGet("webhooks")]
    public async Task<IActionResult> Webhooks(CancellationToken ct) => OrganizationId is Guid id ? Ok(await enterprise.ItemsAsync(id, "webhook", ct)) : Forbid();

    [HttpPost("webhooks")]
    public async Task<IActionResult> Webhook([FromBody] WebhookRequest request, CancellationToken ct)
    {
        if (OrganizationId is not Guid organizationId) return Forbid();
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return BadRequest(new { message = "Use uma URL HTTPS válida." });
        var allowed = new HashSet<string>(["diagnosis.created", "diagnosis.published", "response.received", "diagnosis.completed", "report.generated", "certificate.issued", "action.created", "action.completed", "subscription.updated", "usage.limit_reached"]);
        if (request.Events.Count == 0 || request.Events.Any(x => !allowed.Contains(x))) return BadRequest(new { message = "Selecione eventos suportados." });
        var secret = "whsec_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var config = JsonSerializer.SerializeToElement(new { request.Url, request.Events, secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant(), maxAttempts = 6 });
        var id = await enterprise.SaveItemAsync(organizationId, null, new("webhook", request.Name, "active", config), UserId, ct);
        return Ok(new { id, secret, warning = "Copie o segredo agora. Ele não será exibido novamente." });
    }

    [HttpPost("webhooks/sign")]
    public IActionResult Sign([FromBody] SignRequest request) => Ok(new { signature = WebhookSigner.Sign(request.Secret, request.Payload) });

    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());
    public sealed record ImportRequest(string Type, string Content);
    public sealed record EmailRequest(string Template, string Recipient, JsonElement Payload);
    public sealed record WebhookRequest(string Name, string Url, IReadOnlyList<string> Events);
    public sealed record SignRequest(string Secret, string Payload);
}
