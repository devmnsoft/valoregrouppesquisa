using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs.FreeDiagnostics;
using Valora.Application.Security;

namespace Valora.Application.FreeDiagnostics;

public sealed class FreeDiagnosticsAppService(IFreeDiagnosticsRepository repo, ILogger<FreeDiagnosticsAppService> logger)
{
    static readonly string[] AllowedRoles = ["admin_valora","consultor_valora","empresa_admin"];
    public Task<IReadOnlyList<FreeDiagnosticListItemDto>> ListAsync(FreeDiagnosticFilter filter, ClaimsPrincipal user) => repo.ListAsync(filter, Tenant(user));
    public Task<FreeDiagnosticSummaryDto> SummaryAsync(FreeDiagnosticFilter filter, ClaimsPrincipal user) => repo.SummaryAsync(filter, Tenant(user));
    public async Task<FreeDiagnosticDetailDto> DetailAsync(Guid id, ClaimsPrincipal user) => await repo.DetailAsync(id, Tenant(user)) ?? throw new InvalidOperationException("Diagnóstico gratuito não encontrado ou sem permissão.");

    public async Task<FreeDiagnosticActionResult> ResendAsync(Guid id, ResendResultEmailRequest request, ClaimsPrincipal user, string? correlationId)
    {
        EnsureAllowed(user); var actor = UserId(user); var tenant = Tenant(user); var role = Role(user);
        var force = request.Force && role == "admin_valora" && !string.IsNullOrWhiteSpace(request.Justification);
        var count = await repo.CountManualResendsLast24hAsync(id);
        if (count >= 3 && !force) return new(false,"limited","Limite de 3 reenvios manuais em 24h atingido.",correlationId);
        await repo.CreateResendEmailJobAsync(id, actor, Sanitize(request.Justification ?? "Reenvio operacional"));
        await repo.AuditAsync(tenant,id,actor,"free_survey.result_email_resent","user",JsonSerializer.Serialize(new{ force, reason="manual_resend", justification=Sanitize(request.Justification ?? string.Empty)}));
        logger.LogInformation("Reenvio operacional criado. ResponseId={ResponseId} Actor={Actor} Force={Force}", id, actor, force);
        return new(true,"pending","Reenvio inserido na fila com auditoria LGPD.",correlationId);
    }

    public async Task<FreeDiagnosticActionResult> RegenerateCertificateAsync(Guid id, RegenerateCertificateRequest request, ClaimsPrincipal user, string? correlationId)
    {
        EnsureAllowed(user); var actor = UserId(user); var tenant = Tenant(user);
        await repo.RegenerateCertificateMetadataAsync(id, actor, request.LayoutVersion);
        await repo.AuditAsync(tenant,id,actor,"free_survey.certificate_regenerated","user",JsonSerializer.Serialize(new{ layoutVersion=request.LayoutVersion ?? "current", justification=Sanitize(request.Justification ?? string.Empty)}));
        return new(true,"metadata-ready","Metadados públicos do certificado regenerados sem alterar o resultado original.",correlationId);
    }

    public async Task<FreeDiagnosticActionResult> MarkReviewedAsync(Guid id, MarkCommunicationReviewedRequest request, ClaimsPrincipal user, string? correlationId)
    {
        EnsureAllowed(user); var actor = UserId(user); var note = Sanitize(request.ReviewNote ?? string.Empty);
        await repo.MarkReviewedAsync(id, actor, note);
        await repo.AuditAsync(Tenant(user),id,actor,"free_survey.communication_reviewed","user",JsonSerializer.Serialize(new{ note }));
        return new(true,"reviewed","Falha operacional marcada como revisada.",correlationId);
    }

    static void EnsureAllowed(ClaimsPrincipal user){ if (!AllowedRoles.Contains(Role(user))) throw new UnauthorizedAccessException("Perfil sem permissão para diagnósticos gratuitos."); }
    static string? Role(ClaimsPrincipal u)=>u.FindFirst(ClaimTypes.Role)?.Value ?? u.FindFirst("role")?.Value;
    static Guid? UserId(ClaimsPrincipal u)=>Guid.TryParse(u.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? u.FindFirst("sub")?.Value, out var id)?id:null;
    static string Sanitize(string? value)=>string.IsNullOrWhiteSpace(value)?string.Empty:value.Replace("\r"," ").Replace("\n"," ")[..Math.Min(value.Replace("\r"," ").Replace("\n"," ").Length,240)];
    static Guid? Tenant(ClaimsPrincipal u)=>Role(u)=="empresa_admin" && Guid.TryParse(u.FindFirst("organizationId")?.Value ?? u.FindFirst("organization_id")?.Value, out var id)?id:null;
}
