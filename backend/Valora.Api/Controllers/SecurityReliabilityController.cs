using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[ApiController, Authorize(Roles = "admin_valora,empresa_admin")]
public sealed class SecurityReliabilityController(IDbConnectionFactory connections) : ControllerBase
{
    [HttpGet("/api/v1/security/audit")]
    public Task<IActionResult> Audit(CancellationToken ct) => TenantList(
        "SELECT id,organization_id,actor_user_id,event_type,entity_type,entity_id,action,outcome,correlation_id,occurred_at FROM valorapesquisa.security_audit_events WHERE organization_id=@organizationId ORDER BY occurred_at DESC LIMIT 250", ct);

    [HttpGet("/api/v1/security/access-denials")]
    public Task<IActionResult> Denials(CancellationToken ct) => TenantList(
        "SELECT id,organization_id,user_id,permission_code,resource,reason,correlation_id,occurred_at FROM valorapesquisa.access_denial_events WHERE organization_id=@organizationId ORDER BY occurred_at DESC LIMIT 250", ct);

    [HttpGet("/api/v1/security/sessions")]
    public Task<IActionResult> Sessions(CancellationToken ct) => TenantList(
        "SELECT id,organization_id,user_id,session_id,event_type,correlation_id,occurred_at FROM valorapesquisa.user_session_events WHERE organization_id=@organizationId ORDER BY occurred_at DESC LIMIT 250", ct);

    [HttpGet("/api/v1/security/consents")]
    public Task<IActionResult> Consents(CancellationToken ct) => TenantList(
        "SELECT id,organization_id,user_id,subject_reference,consent_type,consent_version,status,granted_at,revoked_at FROM valorapesquisa.consent_records WHERE organization_id=@organizationId ORDER BY created_at DESC LIMIT 250", ct);

    [HttpGet("/api/v1/privacy/requests")]
    public Task<IActionResult> PrivacyRequests(CancellationToken ct) => TenantList(
        "SELECT id,organization_id,requester_email,request_type,status,justification,assigned_to_user_id,due_at,completed_at,created_at FROM valorapesquisa.data_privacy_requests WHERE organization_id=@organizationId ORDER BY created_at DESC LIMIT 250", ct);

    [HttpPost("/api/v1/privacy/requests")]
    public async Task<IActionResult> CreatePrivacyRequest([FromBody] PrivacyRequestInput input, CancellationToken ct)
    {
        if (!TryTenant(out var organizationId, out var denied)) return denied!;
        if (!new[] { "access", "correction", "export", "anonymization", "erasure" }.Contains(input.RequestType))
            return ValidationProblem("Tipo de solicitação LGPD inválido.");
        using var connection = connections.Create();
        var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            "INSERT INTO valorapesquisa.data_privacy_requests(organization_id,requester_user_id,requester_email,request_type,justification,due_at) VALUES(@organizationId,@userId,@email,@type,@justification,now()+interval '15 days') RETURNING id",
            new { organizationId, userId = UserId(), email = input.RequesterEmail.Trim(), type = input.RequestType, justification = input.Justification.Trim() }, cancellationToken: ct));
        return Created($"/api/v1/privacy/requests/{id}", new { id, status = "open", correlationId = HttpContext.TraceIdentifier });
    }

    [HttpGet("/api/v1/api-keys")]
    public Task<IActionResult> ApiKeys(CancellationToken ct) => TenantList(
        "SELECT id,organization_id,name,key_prefix,scopes,status,expires_at,last_used_at,use_count,created_at,revoked_at FROM valorapesquisa.api_keys WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at DESC", ct);

    [HttpPost("/api/v1/api-keys")]
    public async Task<IActionResult> CreateApiKey([FromBody] ApiKeyInput input, CancellationToken ct)
    {
        if (!TryTenant(out var organizationId, out var denied)) return denied!;
        var rawSecret = $"valora_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret))).ToLowerInvariant();
        var prefix = rawSecret[..15];
        using var connection = connections.Create();
        var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            "INSERT INTO valorapesquisa.api_keys(organization_id,name,key_prefix,key_hash,scopes,status,created_by) VALUES(@organizationId,@name,@prefix,@hash,@scopes,'active',@userId) RETURNING id",
            new { organizationId, name = input.Name.Trim(), prefix, hash, scopes = input.Scopes.Distinct().ToArray(), userId = UserId() }, cancellationToken: ct));
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO valorapesquisa.api_key_events(organization_id,api_key_id,actor_user_id,event_type,correlation_id) VALUES(@organizationId,@id,@userId,'api_key.created',@correlationId)",
            new { organizationId, id, userId = UserId(), correlationId = HttpContext.TraceIdentifier }, cancellationToken: ct));
        return Created("/api/v1/api-keys", new { id, name = input.Name, secret = rawSecret, warning = "Copie agora. O segredo não será exibido novamente.", correlationId = HttpContext.TraceIdentifier });
    }

    [HttpPost("/api/v1/api-keys/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken ct)
    {
        if (!TryTenant(out var organizationId, out var denied)) return denied!;
        using var connection = connections.Create();
        var changed = await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE valorapesquisa.api_keys SET status='revoked',revoked_at=now(),updated_at=now() WHERE id=@id AND organization_id=@organizationId AND status='active' AND deleted_at IS NULL",
            new { id, organizationId }, cancellationToken: ct));
        if (changed == 0) return NotFound(new { message = "Chave ativa não encontrada nesta organização.", correlationId = HttpContext.TraceIdentifier });
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO valorapesquisa.api_key_events(organization_id,api_key_id,actor_user_id,event_type,correlation_id) VALUES(@organizationId,@id,@userId,'api_key.revoked',@correlationId)",
            new { organizationId, id, userId = UserId(), correlationId = HttpContext.TraceIdentifier }, cancellationToken: ct));
        return Ok(new { id, status = "revoked", correlationId = HttpContext.TraceIdentifier });
    }

    [HttpGet("/api/v1/system-health")]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        using var connection = connections.Create();
        var checks = await connection.QueryAsync(new CommandDefinition("SELECT component,status,response_time_ms,friendly_message,correlation_id,checked_at FROM valorapesquisa.system_health_checks ORDER BY checked_at DESC LIMIT 100", cancellationToken: ct));
        return Ok(new { status = "available", checks, correlationId = HttpContext.TraceIdentifier });
    }

    [HttpGet("/api/v1/system-health/errors")]
    public Task<IActionResult> Errors(CancellationToken ct) => TenantList(
        "SELECT id,organization_id,source,severity,friendly_message,correlation_id,occurred_at FROM valorapesquisa.application_error_events WHERE organization_id=@organizationId OR organization_id IS NULL ORDER BY occurred_at DESC LIMIT 250", ct);

    [HttpGet("/api/v1/system-health/jobs")]
    public Task<IActionResult> Jobs(CancellationToken ct) => TenantList(
        "SELECT id,organization_id,job_name,status,correlation_id,started_at,completed_at,duration_ms,retry_of_id FROM valorapesquisa.background_job_runs WHERE organization_id=@organizationId OR organization_id IS NULL ORDER BY started_at DESC LIMIT 250", ct);

    private async Task<IActionResult> TenantList(string sql, CancellationToken ct)
    {
        if (!TryTenant(out var organizationId, out var denied)) return denied!;
        using var connection = connections.Create();
        return Ok(new { data = await connection.QueryAsync(new CommandDefinition(sql, new { organizationId }, cancellationToken: ct)), correlationId = HttpContext.TraceIdentifier });
    }

    private bool TryTenant(out Guid organizationId, out IActionResult? denied)
    {
        var claimOrganization = User.FindFirstValue("organization_id");
        var requestedOrganization = Request.Headers["X-Organization-Id"].FirstOrDefault();
        var isPlatformAdmin = User.IsInRole("admin_valora");
        if (!isPlatformAdmin && !string.IsNullOrWhiteSpace(requestedOrganization) &&
            !string.Equals(requestedOrganization, claimOrganization, StringComparison.OrdinalIgnoreCase))
        {
            organizationId = Guid.Empty;
            denied = Forbid();
            return false;
        }
        var raw = requestedOrganization ?? claimOrganization;
        if (!Guid.TryParse(raw, out organizationId) || organizationId == Guid.Empty)
        {
            denied = BadRequest(new { message = "Selecione uma organização para consultar dados protegidos.", correlationId = HttpContext.TraceIdentifier });
            return false;
        }
        denied = null;
        return true;
    }

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : null;

    public sealed record PrivacyRequestInput(
        [property: System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress] string RequesterEmail,
        [property: System.ComponentModel.DataAnnotations.Required] string RequestType,
        [property: System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(2000)] string Justification);
    public sealed record ApiKeyInput(
        [property: System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(120)] string Name,
        string[] Scopes);
}
