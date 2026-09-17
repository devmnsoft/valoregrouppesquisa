using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.Exceptions;
using Valora.Application.FormalDeliverables;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class FormalDeliverablesController(
    ISecureShareLinkService shares,
    IDiagnosisDocumentSnapshotProvider snapshots,
    IExecutiveReportExportService exporter,
    IDocumentStore documents,
    IFormalDeliverableRepository deliverables,
    IExecutiveDeliveryService delivery) : ControllerBase {
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [Authorize(Policy = ValoraPermissions.Deliverables.Read)]
    [HttpGet("/api/deliverables")]
    public async Task<IActionResult> List([FromQuery] DeliverableListQuery query, CancellationToken ct) =>
        Ok(new { ok = true, data = await delivery.ListAsync(OrganizationId(), query, ct) });

    [Authorize(Policy = ValoraPermissions.Deliverables.Read)]
    [HttpGet("/api/deliverables/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) {
        var item = await delivery.GetAsync(OrganizationId(), id, ct);
        return item is null ? NotFound(new { ok = false, code = "DELIVERABLE_NOT_FOUND" }) : Ok(new { ok = true, data = item });
    }

    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    [HttpPost("/api/deliverables/prepare")]
    public Task<IActionResult> Prepare([FromBody] PrepareDeliverableRequest request, CancellationToken ct) =>
        ExecuteAsync(() => delivery.PrepareAsync(OrganizationId(), RequireUserId(), request, ct));

    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    [HttpPost("/api/deliverables/{id:guid}/submit-review")]
    public Task<IActionResult> SubmitReview(Guid id, [FromBody] SubmitReviewRequest request, CancellationToken ct) =>
        ExecuteAsync(() => delivery.SubmitForReviewAsync(OrganizationId(), RequireUserId(), id, request, ct));

    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    [HttpPost("/api/deliverables/{id:guid}/publish")]
    public Task<IActionResult> Publish(Guid id, [FromBody] PublishDeliverableRequest request, CancellationToken ct) =>
        ExecuteAsync(() => delivery.PublishAsync(OrganizationId(), RequireUserId(), id, request, ct));

    [Authorize(Policy = ValoraPermissions.Reports.Download)]
    [HttpGet("/api/deliverables/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct) {
        try {
            var file = await delivery.DownloadAsync(OrganizationId(), RequireUserId(), id, ct);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (NotFoundAppException) { return NotFound(new { ok = false, code = "DELIVERABLE_NOT_FOUND" }); }
        catch (ForbiddenAppException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { ok = false, message = ex.Message }); }
    }

    [Authorize(Policy = ValoraPermissions.ShareLinks.Manage)]
    [HttpPost("/api/deliverables/{id:guid}/secure-share")]
    public async Task<IActionResult> ShareDeliverable(Guid id, [FromBody] CreateDeliverableShareRequest request, CancellationToken ct) {
        try {
            var created = await delivery.ShareAsync(OrganizationId(), RequireUserId(), id, request, ct);
            return Created($"/entregas/publica/{created.Token}", new {
                created.Id,
                created.ExpiresAt,
                created.AllowDownload,
                token = created.Token,
                url = $"{Request.Scheme}://{Request.Host}/entregas/publica/{created.Token}",
                notice = "O token bruto é exibido somente agora e não pode ser recuperado depois. Revogue e crie um novo link se necessário."
            });
        }
        catch (NotFoundAppException) { return NotFound(new { ok = false, code = "DELIVERABLE_NOT_FOUND" }); }
        catch (ValidationAppException ex) { return BadRequest(new { ok = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { ok = false, message = ex.Message }); }
    }

    [Authorize(Policy = ValoraPermissions.Certificates.Read)]
    [HttpGet("/api/deliverables/certificate-eligibility")]
    public async Task<IActionResult> CertificateEligibility([FromQuery] Guid resultId, [FromQuery] string? templateCode, CancellationToken ct) =>
        Ok(new { ok = true, data = await delivery.CertificateEligibilityAsync(OrganizationId(), resultId, templateCode, ct) });

    [Authorize]
    [HttpPost("/api/deliverables/{diagnosisId:guid}/share-links")]
    public async Task<IActionResult> Create(Guid diagnosisId, [FromBody] CreateShareLinkRequest request, CancellationToken ct) {
        var organizationId = OrganizationId();
        var created = await shares.CreateAsync(organizationId, diagnosisId, UserId,
            TimeSpan.FromHours(request.ValidForHours is > 0 and <= 2160 ? request.ValidForHours : 72), request.AllowDownload, ct);
        return Created($"/share/{created.PublicSlug}", new {
            created.Id,
            created.ExpiresAt,
            created.AllowDownload,
            token = created.Token,
            url = $"{Request.Scheme}://{Request.Host}/share/{created.PublicSlug}",
            notice = "O token bruto é exibido somente agora e não pode ser recuperado depois."
        });
    }

    [Authorize]
    [HttpDelete("/api/share-links/{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct) {
        var organizationId = OrganizationId();
        return await shares.RevokeAsync(organizationId, id, UserId, ct) ? NoContent() : NotFound();
    }

    [AllowAnonymous]
    [HttpGet("/share/{slug}")]
    [HttpGet("/p/r/{slug}")]
    public async Task<IActionResult> PublicResult(string slug, CancellationToken ct) {
        var link = await shares.ResolveAsync(slug, false, ct);
        if (link is null) return NotFound(new { ok = false, code = "SHARE_LINK_UNAVAILABLE", message = "Este link expirou, foi revogado ou não existe." });

        if (link.DeliverableId is Guid deliverableId) {
            var details = await deliverables.GetDetailsAsync(link.OrganizationId, deliverableId, ct);
            if (details is null || !string.Equals(details.EditorialStatus, DeliverableEditorialStatuses.Published, StringComparison.Ordinal))
                return NotFound(new { ok = false, code = "DOCUMENT_UNAVAILABLE", message = "Documento indisponível." });
            return Ok(new {
                ok = true,
                title = details.Title,
                diagnosis = details.DiagnosticName,
                version = details.VersionNumber,
                summary = details.ExecutiveNotes,
                sections = details.Sections,
                limitations = details.Limitations,
                methodologyVersion = details.MethodologyVersion,
                publishedAt = details.PublishedAt,
                link.AllowDownload,
                link.ExpiresAt
            });
        }

        var snapshot = await snapshots.LoadAsync(link.OrganizationId, link.DiagnosisId, ct);
        if (snapshot is null) return NotFound(new { ok = false, code = "RESULT_NOT_FOUND" });
        return Ok(new {
            ok = true,
            title = snapshot.DiagnosisName,
            organization = snapshot.OrganizationName,
            score = snapshot.OverallScore,
            maturityLevel = snapshot.MaturityLevel,
            summary = snapshot.ExecutiveSummary,
            indexes = snapshot.Dimensions,
            recommendations = snapshot.Recommendations.Take(5),
            link.AllowDownload,
            link.ExpiresAt
        });
    }

    [AllowAnonymous]
    [HttpGet("/share/{slug}/download")]
    [HttpGet("/p/r/{slug}/download")]
    public async Task<IActionResult> PublicDownload(string slug, CancellationToken ct) {
        var link = await shares.ResolveAsync(slug, true, ct);
        if (link is null) return NotFound(new { ok = false, code = "DOWNLOAD_NOT_ALLOWED", message = "Download indisponível para este link." });

        if (link.DeliverableId is Guid deliverableId) {
            var details = await deliverables.GetDetailsAsync(link.OrganizationId, deliverableId, ct);
            if (details?.DocumentId is not Guid documentId) {
                return NotFound(new { ok = false, code = "FILE_UNAVAILABLE", message = "Arquivo publicado indisponível." });
            }
            var stored = await documents.FindAsync(link.OrganizationId, documentId, ct);
            if (stored is null || stored.Content.Length == 0) {
                return NotFound(new { ok = false, code = "FILE_UNAVAILABLE", message = "Arquivo publicado indisponível." });
            }
            return File(stored.Content, stored.ContentType, stored.FileName);
        }

        // Legacy share links without deliverable keep snapshot export for compatibility.
        var snapshot = await snapshots.LoadAsync(link.OrganizationId, link.DiagnosisId, ct);
        if (snapshot is null) return NotFound();
        var document = exporter.Render(snapshot, DeliverableFormat.Pdf, DateTimeOffset.UtcNow);
        return File(document.Content, document.ContentType, document.FileName);
    }

    private Guid OrganizationId() => Guid.TryParse(User.FindFirstValue("organization_id"), out var id)
        ? id : throw new UnauthorizedAccessException("Contexto de organização ausente.");

    private Guid RequireUserId() => UserId ?? throw new UnauthorizedAccessException("Usuário autenticado obrigatório.");

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action) {
        try {
            return Ok(new { ok = true, data = await action() });
        }
        catch (ValidationAppException ex) { return BadRequest(new { ok = false, message = ex.Message }); }
        catch (NotFoundAppException) { return NotFound(new { ok = false, code = "DELIVERABLE_NOT_FOUND" }); }
        catch (ForbiddenAppException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { ok = false, message = ex.Message }); }
    }
}

public sealed record CreateShareLinkRequest(int ValidForHours = 72, bool AllowDownload = false);
