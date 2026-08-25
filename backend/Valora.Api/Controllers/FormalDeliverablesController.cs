using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.FormalDeliverables;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class FormalDeliverablesController(
    ISecureShareLinkService shares,
    IDiagnosisDocumentSnapshotProvider snapshots,
    IExecutiveReportExportService exporter) : ControllerBase
{
    [Authorize]
    [HttpPost("/api/deliverables/{diagnosisId:guid}/share-links")]
    public async Task<IActionResult> Create(Guid diagnosisId, [FromBody] CreateShareLinkRequest request, CancellationToken ct)
    {
        var organizationId = OrganizationId();
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
        var created = await shares.CreateAsync(organizationId, diagnosisId, userId,
            TimeSpan.FromHours(request.ValidForHours is > 0 and <= 2160 ? request.ValidForHours : 72), request.AllowDownload, ct);
        return Created($"/share/{created.PublicSlug}", new
        {
            created.Id, created.ExpiresAt, created.AllowDownload,
            url = $"{Request.Scheme}://{Request.Host}/share/{created.PublicSlug}"
        });
    }

    [Authorize]
    [HttpDelete("/api/share-links/{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var organizationId = OrganizationId();
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : (Guid?)null;
        return await shares.RevokeAsync(organizationId, id, userId, ct) ? NoContent() : NotFound();
    }

    [AllowAnonymous]
    [HttpGet("/share/{slug}")]
    [HttpGet("/p/r/{slug}")]
    public async Task<IActionResult> PublicResult(string slug, CancellationToken ct)
    {
        var link = await shares.ResolveAsync(slug, false, ct);
        if (link is null) return NotFound(new { ok = false, code = "SHARE_LINK_UNAVAILABLE", message = "Este link expirou, foi revogado ou não existe." });
        var snapshot = await snapshots.LoadAsync(link.OrganizationId, link.DiagnosisId, ct);
        if (snapshot is null) return NotFound(new { ok = false, code = "RESULT_NOT_FOUND" });
        return Ok(new { ok = true, title = snapshot.DiagnosisName, organization = snapshot.OrganizationName,
            score = snapshot.OverallScore, maturityLevel = snapshot.MaturityLevel, summary = snapshot.ExecutiveSummary,
            indexes = snapshot.Dimensions, recommendations = snapshot.Recommendations.Take(5), link.AllowDownload, link.ExpiresAt });
    }

    [AllowAnonymous]
    [HttpGet("/share/{slug}/download")]
    [HttpGet("/p/r/{slug}/download")]
    public async Task<IActionResult> PublicDownload(string slug, CancellationToken ct)
    {
        var link = await shares.ResolveAsync(slug, true, ct);
        if (link is null) return NotFound(new { ok = false, code = "DOWNLOAD_NOT_ALLOWED", message = "Download indisponível para este link." });
        var snapshot = await snapshots.LoadAsync(link.OrganizationId, link.DiagnosisId, ct);
        if (snapshot is null) return NotFound();
        var document = exporter.Render(snapshot, DeliverableFormat.Pdf, DateTimeOffset.UtcNow);
        return File(document.Content, document.ContentType, document.FileName);
    }

    private Guid OrganizationId() => Guid.TryParse(User.FindFirstValue("organization_id"), out var id)
        ? id : throw new UnauthorizedAccessException("Contexto de organização ausente.");
}

public sealed record CreateShareLinkRequest(int ValidForHours = 72, bool AllowDownload = false);
