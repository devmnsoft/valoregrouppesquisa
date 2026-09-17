using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.Common;
using Valora.Application.Exceptions;
using Valora.Application.FormalDeliverables;
using Valora.Application.Workspace;

namespace Valora.Web.Controllers;

[Authorize(Policy = ValoraPermissions.Reports.Read)]
[Route("Reports")]
public sealed class ReportsController(
    IExecutiveDeliveryService delivery,
    ICurrentRequestContext requestContext) : Controller {
    private CurrentRequestContext Context => requestContext.GetCurrent();

    private bool CanGenerate => Context.IsGlobalAdministrator
        || Context.Permissions.Contains(ValoraPermissions.Reports.Generate, StringComparer.OrdinalIgnoreCase);

    private bool CanDownload => Context.IsGlobalAdministrator
        || Context.Permissions.Contains(ValoraPermissions.Reports.Download, StringComparer.OrdinalIgnoreCase);

    private bool CanShare => Context.IsGlobalAdministrator
        || Context.Permissions.Contains(ValoraPermissions.ShareLinks.Manage, StringComparer.OrdinalIgnoreCase);

    private bool TryScope(out Guid organizationId, out Guid userId) {
        var context = Context;
        organizationId = context.EffectiveOrganizationId ?? Guid.Empty;
        userId = context.UserId ?? Guid.Empty;
        return organizationId != Guid.Empty && userId != Guid.Empty;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] DeliverableListQuery query, Guid? reportId, CancellationToken cancellationToken) {
        if (reportId is Guid legacyId && legacyId != Guid.Empty)
            return RedirectToAction(nameof(Details), new { id = legacyId });
        if (!TryScope(out var organizationId, out _))
            return View("MissingOrganization");

        var page = await delivery.ListAsync(organizationId, query, cancellationToken);
        return View(new DeliverableListPageViewModel(page, query, CurrentUrl(), CanGenerate, null));
    }

    [HttpGet("Prepare")]
    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    public IActionResult Prepare(string? returnUrl) {
        if (!TryScope(out _, out _))
            return View("MissingOrganization");
        ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl, "/Reports", "/Reports");
        var command = new PrepareDeliverableRequest {
            TemplateCode = "executive_valora",
            Title = "",
            Sections = DefaultSections,
            CommandId = Guid.NewGuid().ToString("N")
        };
        return View(new DeliverablePreparePageViewModel(command, null, null, null));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Prepare")]
    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    public async Task<IActionResult> Prepare(PrepareDeliverableRequest command, string? resultDisplayName, string? diagnosticDisplayName, string? reviewerDisplayName, string? returnUrl, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out var userId))
            return Forbid();
        ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl, "/Reports", "/Reports");
        if (!ModelState.IsValid)
            return View(new DeliverablePreparePageViewModel(command, resultDisplayName, diagnosticDisplayName, reviewerDisplayName));

        try {
            var created = await delivery.PrepareAsync(organizationId, userId, command, cancellationToken);
            TempData["DeliverableSuccess"] = "Entregável preparado como rascunho. Revise as seções antes de publicar.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (ValidationAppException ex) {
            ModelState.AddModelError("", ex.Message);
        }
        catch (InvalidOperationException ex) {
            ModelState.AddModelError("", FriendlyModuleMessage(ex.Message));
        }
        catch (ForbiddenAppException) {
            return Forbid();
        }
        catch (NotFoundAppException ex) {
            ModelState.AddModelError("", ex.Message);
        }

        return View(new DeliverablePreparePageViewModel(command, resultDisplayName, diagnosticDisplayName, reviewerDisplayName));
    }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string? returnUrl, string? openDialog = null, CancellationToken cancellationToken = default) {
        if (!TryScope(out var organizationId, out _))
            return View("MissingOrganization");
        var deliverable = await delivery.GetAsync(organizationId, id, cancellationToken);
        if (deliverable is null)
            return NotFound();
        return View(BuildDetailsPage(deliverable, SafeReturnUrl(returnUrl, "/Reports", "/Reports"), openDialog, null));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Details/{id:guid}/SubmitReview")]
    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    public async Task<IActionResult> SubmitReview(Guid id, SubmitReviewRequest command, string? returnUrl, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out var userId))
            return Forbid();
        var back = SafeReturnUrl(returnUrl, "/Reports", "/Reports");
        if (!ModelState.IsValid)
            return await DetailsWithCommand(organizationId, id, back, "submit-review-dialog", command, cancellationToken);

        try {
            await delivery.SubmitForReviewAsync(organizationId, userId, id, command, cancellationToken);
            TempData["DeliverableSuccess"] = "Entregável enviado para revisão.";
            return RedirectToAction(nameof(Details), new { id, returnUrl = back });
        }
        catch (Exception ex) when (ex is ValidationAppException or InvalidOperationException) {
            ModelState.AddModelError("", FriendlyModuleMessage(ex.Message));
            return await DetailsWithCommand(organizationId, id, back, "submit-review-dialog", command, cancellationToken);
        }
        catch (ForbiddenAppException) {
            return Forbid();
        }
        catch (NotFoundAppException) {
            return NotFound();
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Details/{id:guid}/Publish")]
    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    public async Task<IActionResult> Publish(Guid id, PublishDeliverableRequest command, string? returnUrl, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out var userId))
            return Forbid();
        var back = SafeReturnUrl(returnUrl, "/Reports", "/Reports");
        if (!ModelState.IsValid)
            return await DetailsWithCommand(organizationId, id, back, "publish-dialog", command, cancellationToken);

        try {
            await delivery.PublishAsync(organizationId, userId, id, command, cancellationToken);
            TempData["DeliverableSuccess"] = "Entregável publicado. O arquivo gerado fica disponível para download autorizado.";
            return RedirectToAction(nameof(Details), new { id, returnUrl = back });
        }
        catch (Exception ex) when (ex is ValidationAppException or InvalidOperationException) {
            ModelState.AddModelError("", FriendlyModuleMessage(ex.Message));
            return await DetailsWithCommand(organizationId, id, back, "publish-dialog", command, cancellationToken);
        }
        catch (ForbiddenAppException) {
            return Forbid();
        }
        catch (NotFoundAppException) {
            return NotFound();
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Details/{id:guid}/NewVersion")]
    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    public async Task<IActionResult> NewVersion(Guid id, string commandId, string? returnUrl, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out var userId))
            return Forbid();
        var back = SafeReturnUrl(returnUrl, "/Reports", "/Reports");
        if (string.IsNullOrWhiteSpace(commandId)) {
            ModelState.AddModelError("", "Informe a chave da operação.");
            return await DetailsWithCommand(organizationId, id, back, "new-version-dialog", null, cancellationToken);
        }

        try {
            var created = await delivery.CreateNewVersionAsync(organizationId, userId, id, commandId, cancellationToken);
            TempData["DeliverableSuccess"] = "Nova versão criada como rascunho a partir do entregável publicado.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (Exception ex) when (ex is ValidationAppException or InvalidOperationException) {
            ModelState.AddModelError("", FriendlyModuleMessage(ex.Message));
            return await DetailsWithCommand(organizationId, id, back, "new-version-dialog", null, cancellationToken);
        }
        catch (ForbiddenAppException) {
            return Forbid();
        }
        catch (NotFoundAppException) {
            return NotFound();
        }
    }

    [HttpGet("Details/{id:guid}/Download")]
    [Authorize(Policy = ValoraPermissions.Reports.Download)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out var userId))
            return Forbid();

        try {
            var document = await delivery.DownloadAsync(organizationId, userId, id, cancellationToken);
            return File(document.Content, document.ContentType, document.FileName);
        }
        catch (NotFoundAppException) {
            return NotFound();
        }
        catch (ForbiddenAppException) {
            return Forbid();
        }
        catch (InvalidOperationException ex) {
            TempData["DeliverableError"] = FriendlyModuleMessage(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Details/{id:guid}/Share")]
    [Authorize(Policy = ValoraPermissions.ShareLinks.Manage)]
    public async Task<IActionResult> Share(Guid id, CreateDeliverableShareRequest command, string? returnUrl, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out var userId))
            return Forbid();
        var back = SafeReturnUrl(returnUrl, "/Reports", "/Reports");
        if (!ModelState.IsValid)
            return await DetailsWithCommand(organizationId, id, back, "share-dialog", command, cancellationToken);

        try {
            var created = await delivery.ShareAsync(organizationId, userId, id, command, cancellationToken);
            TempData["DeliverableSuccess"] = "Link seguro criado. Copie o token agora; ele não poderá ser recuperado depois.";
            TempData["ShareToken"] = created.Token;
            TempData["SharePublicUrl"] = $"{Request.Scheme}://{Request.Host}/entregas/publica/{created.Token}";
            return RedirectToAction(nameof(Details), new { id, returnUrl = back });
        }
        catch (Exception ex) when (ex is ValidationAppException or InvalidOperationException) {
            ModelState.AddModelError("", FriendlyModuleMessage(ex.Message));
            return await DetailsWithCommand(organizationId, id, back, "share-dialog", command, cancellationToken);
        }
        catch (ForbiddenAppException) {
            return Forbid();
        }
        catch (NotFoundAppException) {
            return NotFound();
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Share/{linkId:guid}/Revoke")]
    [Authorize(Policy = ValoraPermissions.ShareLinks.Manage)]
    public async Task<IActionResult> RevokeShare(Guid linkId, Guid deliverableId, string? returnUrl, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out var userId))
            return Forbid();
        var back = SafeReturnUrl(returnUrl, "/Reports", "/Reports");

        try {
            var revoked = await delivery.RevokeShareAsync(organizationId, userId, linkId, cancellationToken);
            TempData["DeliverableSuccess"] = revoked
                ? "Link de compartilhamento revogado."
                : "O link já estava indisponível ou não foi encontrado.";
            return RedirectToAction(nameof(Details), new { id = deliverableId, returnUrl = back });
        }
        catch (ForbiddenAppException) {
            return Forbid();
        }
        catch (NotFoundAppException) {
            return NotFound();
        }
    }

    [HttpGet("ResultOptions")]
    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    public async Task<IActionResult> ResultOptions(string? search, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out _))
            return Forbid();
        return Json(await delivery.EligibleResultsAsync(organizationId, search, cancellationToken));
    }

    [HttpGet("ReviewerOptions")]
    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    public async Task<IActionResult> ReviewerOptions(string? search, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out _))
            return Forbid();
        return Json(await delivery.ReviewersAsync(organizationId, search, cancellationToken));
    }

    [HttpGet("TemplateOptions")]
    [Authorize(Policy = ValoraPermissions.Reports.Generate)]
    public async Task<IActionResult> TemplateOptions(string? type, CancellationToken cancellationToken) {
        if (!TryScope(out _, out _))
            return Forbid();
        return Json(await delivery.TemplatesAsync(type, cancellationToken));
    }

    [HttpGet("CertificateEligibility")]
    [Authorize(Policy = ValoraPermissions.Reports.Read)]
    public async Task<IActionResult> CertificateEligibility(Guid resultId, string? templateCode, CancellationToken cancellationToken) {
        if (!TryScope(out var organizationId, out _))
            return Forbid();
        return Json(await delivery.CertificateEligibilityAsync(organizationId, resultId, templateCode, cancellationToken));
    }

    private async Task<IActionResult> DetailsWithCommand(Guid organizationId, Guid id, string returnUrl, string dialog, object? command, CancellationToken cancellationToken) {
        var deliverable = await delivery.GetAsync(organizationId, id, cancellationToken);
        if (deliverable is null)
            return NotFound();
        return View("Details", BuildDetailsPage(deliverable, returnUrl, dialog, command));
    }

    private DeliverableDetailsPageViewModel BuildDetailsPage(DeliverableDetailsDto deliverable, string returnUrl, string? openDialog, object? command) {
        var capabilities = new DeliverableCapabilities(
            CanReview: CanGenerate && deliverable.EditorialStatus == DeliverableEditorialStatuses.Draft,
            CanPublish: CanGenerate && (
                deliverable.EditorialStatus == DeliverableEditorialStatuses.InReview
                || deliverable.EditorialStatus == DeliverableEditorialStatuses.Draft),
            CanDownload: CanDownload && deliverable.FileAvailable,
            CanShare: CanShare
                && deliverable.EditorialStatus == DeliverableEditorialStatuses.Published
                && deliverable.ProcessingStatus == DeliverableProcessingStatuses.Available,
            CanNewVersion: CanGenerate && deliverable.EditorialStatus == DeliverableEditorialStatuses.Published);

        return new DeliverableDetailsPageViewModel(
            deliverable,
            capabilities,
            openDialog,
            command,
            returnUrl,
            TempData["ShareToken"] as string,
            TempData["SharePublicUrl"] as string);
    }

    private string CurrentUrl() => Request.Path + Request.QueryString;

    private string SafeReturnUrl(string? value, string fallback, params string[] allowedPaths) {
        if (string.IsNullOrWhiteSpace(value) || !Url.IsLocalUrl(value))
            return fallback;
        var path = value.Split('?', 2)[0];
        return allowedPaths.Any(allowed =>
            allowed.EndsWith('/')
                ? path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
                : path.Equals(allowed, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(allowed + "/", StringComparison.OrdinalIgnoreCase))
            ? value
            : fallback;
    }

    private static string FriendlyModuleMessage(string message) =>
        string.Equals(message, "MODULE_NOT_ENABLED", StringComparison.Ordinal)
            ? "O módulo de relatórios ou certificados não está habilitado para esta organização."
            : message;

    private static readonly string[] DefaultSections = [
        "objective", "period", "participation", "results", "dimensions",
        "evidence", "limitations", "recommendations", "action_plans"
    ];
}

public sealed record DeliverableCapabilities(
    bool CanReview,
    bool CanPublish,
    bool CanDownload,
    bool CanShare,
    bool CanNewVersion);

public sealed record DeliverableListPageViewModel(
    PageResult<DeliverableListItemDto> Page,
    DeliverableListQuery Query,
    string CurrentUrl,
    bool CanPrepare,
    string? OrganizationLabel);

public sealed record DeliverablePreparePageViewModel(
    PrepareDeliverableRequest Command,
    string? ResultDisplayName,
    string? DiagnosticDisplayName,
    string? ReviewerDisplayName);

public sealed record DeliverableDetailsPageViewModel(
    DeliverableDetailsDto Deliverable,
    DeliverableCapabilities Capabilities,
    string? OpenDialog,
    object? Command,
    string ReturnUrl,
    string? ShareToken,
    string? SharePublicUrl);
