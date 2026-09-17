using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.FormalDeliverables;

namespace Valora.Web.Controllers;

/// <summary>
/// HTML pública de entregáveis compartilhados.
/// Rotas Web: /entregas/publica/{slug} e /p/e/{slug} (e respectivos /download).
/// O host da API mantém JSON em /share/{slug} e /p/r/{slug}; não conflitar com essas rotas no Web.
/// </summary>
[AllowAnonymous]
[Route("entregas/publica")]
[Route("p/e")]
public sealed class PublicDeliverablesController(
    ISecureShareLinkService shares,
    IShareLinkRepository shareLinks,
    IFormalDeliverableRepository deliverables,
    IDiagnosisDocumentSnapshotProvider snapshots,
    IDocumentStore documents,
    ILogger<PublicDeliverablesController> logger) : Controller {
    [HttpGet("{slug}")]
    public async Task<IActionResult> Show(string slug, CancellationToken cancellationToken) {
        ViewData["Title"] = "Entrega Valora Insight™";
        try {
            var classified = await ClassifyAsync(slug, downloadRequested: false, cancellationToken);
            if (classified.State != PublicDeliverableState.Available) {
                return View("Public", new PublicDeliverablePageViewModel(classified.State, null, null, false));
            }

            // Resolve once: registers access history without double-counting.
            var link = await shares.ResolveAsync(slug, false, cancellationToken);
            if (link is null) {
                return View("Public", new PublicDeliverablePageViewModel(PublicDeliverableState.Unavailable, null, null, false));
            }

            if (link.DeliverableId is Guid deliverableId) {
                var details = await deliverables.GetDetailsAsync(link.OrganizationId, deliverableId, cancellationToken);
                if (details is null
                    || !string.Equals(details.EditorialStatus, DeliverableEditorialStatuses.Published, StringComparison.Ordinal)) {
                    return View("Public", new PublicDeliverablePageViewModel(PublicDeliverableState.Unavailable, null, null, false));
                }

                // Snapshot provider expects result id (parameter historically named DiagnosisId).
                var snapshot = details.ResultId is Guid resultId
                    ? await snapshots.LoadAsync(link.OrganizationId, resultId, cancellationToken)
                    : null;
                return View("Public", new PublicDeliverablePageViewModel(
                    PublicDeliverableState.Available,
                    details,
                    snapshot,
                    link.AllowDownload && details.FileAvailable));
            }

            var legacy = await snapshots.LoadAsync(link.OrganizationId, link.DiagnosisId, cancellationToken);
            if (legacy is null) {
                return View("Public", new PublicDeliverablePageViewModel(PublicDeliverableState.Unavailable, null, null, false));
            }

            return View("Public", new PublicDeliverablePageViewModel(
                PublicDeliverableState.Available,
                null,
                legacy,
                link.AllowDownload));
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Falha temporária ao abrir entrega pública.");
            return View("Public", new PublicDeliverablePageViewModel(PublicDeliverableState.TemporaryFailure, null, null, false));
        }
    }

    [HttpGet("{slug}/download")]
    public async Task<IActionResult> Download(string slug, CancellationToken cancellationToken) {
        try {
            var classified = await ClassifyAsync(slug, downloadRequested: true, cancellationToken);
            if (classified.State != PublicDeliverableState.Available || classified.Link is null || !classified.Link.AllowDownload) {
                return classified.State is PublicDeliverableState.Invalid or PublicDeliverableState.Expired or PublicDeliverableState.Revoked
                    ? NotFound()
                    : NotFound();
            }

            var link = await shares.ResolveAsync(slug, true, cancellationToken);
            if (link is null || !link.AllowDownload || link.DeliverableId is not Guid deliverableId) {
                return NotFound();
            }

            var details = await deliverables.GetDetailsAsync(link.OrganizationId, deliverableId, cancellationToken);
            if (details?.DocumentId is not Guid documentId) {
                return NotFound();
            }

            var stored = await documents.FindAsync(link.OrganizationId, documentId, cancellationToken);
            if (stored is null || stored.Content.Length == 0) {
                return NotFound();
            }

            return File(stored.Content, stored.ContentType, stored.FileName);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Falha temporária no download público de entrega.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private async Task<(PublicDeliverableState State, ShareLink? Link)> ClassifyAsync(string slug, bool downloadRequested, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length < 40) {
            return (PublicDeliverableState.Invalid, null);
        }

        var link = await shareLinks.FindAnyByHashAsync(Hash(slug), cancellationToken);
        if (link is null) {
            return (PublicDeliverableState.Invalid, null);
        }

        if (link.RevokedAt.HasValue) {
            return (PublicDeliverableState.Revoked, link);
        }

        if (link.ExpiresAt <= DateTimeOffset.UtcNow) {
            return (PublicDeliverableState.Expired, link);
        }

        if (link.MaxAccessCount.HasValue && link.AccessCount >= link.MaxAccessCount.Value) {
            return (PublicDeliverableState.Unavailable, link);
        }

        if (downloadRequested && !link.AllowDownload) {
            return (PublicDeliverableState.Unavailable, link);
        }

        return (PublicDeliverableState.Available, link);
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}

public enum PublicDeliverableState {
    Available,
    Invalid,
    Expired,
    Revoked,
    Unavailable,
    TemporaryFailure
}

public sealed record PublicDeliverablePageViewModel(
    PublicDeliverableState State,
    DeliverableDetailsDto? Deliverable,
    DiagnosisDocumentSnapshot? Snapshot,
    bool CanDownload);
