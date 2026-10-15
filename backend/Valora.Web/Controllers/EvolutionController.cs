using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.Common;
using Valora.Application.Evolution;
using Valora.Application.Contracts;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize(Policy = ValoraPermissions.Evolution.Read)]
[Route("Evolution")]
public sealed class EvolutionController(
    IEvolutionCycleService cycles,
    EvolutionSnapshotService snapshots,
    ICurrentOrganizationProvider organizationProvider, IOrganizationRepository organizations) : Controller {
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    [HttpGet("")]
    public IActionResult Index() {
        var organization = organizationProvider.GetCurrent();
        return organization.IsResolved
            ? RedirectToAction(nameof(Cycles))
            : OrganizationRequired();
    }

    [HttpGet("Cycles")]
    public async Task<IActionResult> Cycles(Guid? diagnosticId, Guid? resultId, string? returnUrl, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        var zone = await LoadTimeZone(organization.RequireOrganizationId(), cancellationToken);
        var command = ViewData["CycleCommand"] as EvolutionCycleViewModel ?? new EvolutionCycleViewModel {
            DiagnosticId = diagnosticId,
            ResultId = resultId,
            PeriodStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date,
            EvidenceSummary = string.Empty
        };
        ViewData["CycleCommand"] = command;
        ViewData["ResultOptions"] = await cycles.ResultOptions(organization.RequireOrganizationId(), diagnosticId, cancellationToken);
        ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl, "/Evolution", "/Diagnostics/", "/ActionCenter/", "/Results/");
        return View(await cycles.List(organization.RequireOrganizationId(), cancellationToken));
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/Open"), Authorize(Policy = ValoraPermissions.Evolution.Manage)]
    public async Task<IActionResult> Open(EvolutionCycleViewModel model, string? returnUrl, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        await LoadTimeZone(organization.RequireOrganizationId(), cancellationToken);
        var resultOptions = (await cycles.ResultOptions(organization.RequireOrganizationId(), model.DiagnosticId, cancellationToken)).ToList();
        if (model.SourceSurveyId is { } selected && resultOptions.All(x => x.SourceSurveyId != selected) && await cycles.SelectedOption(organization.RequireOrganizationId(), selected, cancellationToken) is { } option) resultOptions.Add(option);
        if (model.PeriodEnd.HasValue && model.PeriodEnd.Value < model.PeriodStart)
            ModelState.AddModelError(nameof(model.PeriodEnd), "A data final deve ser posterior à data inicial.");
        if (!ModelState.IsValid) {
            TempData["EvolutionError"] = "Revise os campos destacados antes de continuar.";
            ViewData["CycleCommand"] = model;
            ViewData["ResultOptions"] = resultOptions;
            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl, "/Evolution", "/Diagnostics/", "/ActionCenter/", "/Results/");
            return View("Cycles", await cycles.List(organization.RequireOrganizationId(), cancellationToken));
        }
        var request = new OpenEvolutionCycleRequest(model.DiagnosticId, model.ResultId, null, model.Title, model.Summary,
            model.BaselineScore, model.TargetScore, null, model.PeriodStart, model.PeriodEnd, model.EvidenceSummary, SourceSurveyId: model.SourceSurveyId, ReferenceMode: model.ReferenceMode);
        Guid id;
        try { id = await cycles.Open(organization.RequireOrganizationId(), UserId, request, cancellationToken); }
        catch (ArgumentException error) { ModelState.AddModelError("", error.Message); TempData["EvolutionError"] = "Revise os campos destacados antes de continuar."; ViewData["CycleCommand"] = model; ViewData["ResultOptions"] = resultOptions; ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl, "/Evolution", "/Diagnostics/", "/ActionCenter/", "/Results/"); return View("Cycles", await cycles.List(organization.RequireOrganizationId(), cancellationToken)); }
        return RedirectToAction(nameof(Details), new { id, returnUrl = SafeReturnUrl(returnUrl, "/Evolution", "/Diagnostics/", "/ActionCenter/", "/Results/") });
    }

    [HttpGet("Cycles/Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string? returnUrl, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        var cycle = await cycles.Get(organization.RequireOrganizationId(), id, cancellationToken);
        return cycle is null
            ? NotFound()
            : View(await DetailsModel(organization.RequireOrganizationId(), cycle, SafeReturnUrl(returnUrl, "/Evolution/Cycles", "/Evolution/", "/Diagnostics/", "/ActionCenter/", "/Results/"), cancellationToken));
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/{id:guid}/Snapshot"), Authorize(Policy = ValoraPermissions.Evolution.SnapshotsGenerate)]
    public async Task<IActionResult> Snapshot(Guid id, EvolutionSnapshotViewModel model, string? returnUrl, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        var safeReturnUrl = SafeReturnUrl(returnUrl, "/Evolution/Cycles", "/Diagnostics/", "/ActionCenter/", "/Results/");
        if (!ModelState.IsValid) {
            var cycle = await cycles.Get(organization.RequireOrganizationId(), id, cancellationToken);
            if (cycle is null) return NotFound();
            ViewData["SnapshotCommand"] = model;
            return View("Details", await DetailsModel(organization.RequireOrganizationId(), cycle, safeReturnUrl, cancellationToken));
        }
        try { await snapshots.Generate(organization.RequireOrganizationId(), UserId, id, model.Evidence, model.Interpretation, model.Recommendation, cancellationToken); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException) { ModelState.AddModelError("", error.Message); var cycle = await cycles.Get(organization.RequireOrganizationId(), id, cancellationToken); if (cycle is null) return NotFound(); ViewData["SnapshotCommand"] = model; return View("Details", await DetailsModel(organization.RequireOrganizationId(), cycle, safeReturnUrl, cancellationToken)); }
        TempData["EvolutionSuccess"] = "Leitura preservada. Execução e mudança observada permanecem separadas.";
        return RedirectToAction(nameof(Details), new { id, returnUrl = safeReturnUrl });
    }

    private static string SafeReturnUrl(string? value, string fallback, params string[] allowedPrefixes) {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("/", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal) || value.Contains('\\') || value.Any(char.IsControl)) return fallback;
        var path = value.Split('?', 2)[0];
        if (path is "/Evolution" or "/Evolution/Cycles") return value;
        return allowedPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ? value : fallback;
    }

    [HttpGet("MeasurementOptions")]
    public async Task<IActionResult> MeasurementOptions(Guid? diagnosticId, int page = 1, CancellationToken c = default) =>
        Json(await cycles.SelectionPage(organizationProvider.GetCurrent().RequireOrganizationId(), diagnosticId, page, c));

    [ValidateAntiForgeryToken, HttpPost("Cycles/{id:guid}/ReviewReference"), Authorize(Policy = ValoraPermissions.Evolution.Manage)]
    public async Task<IActionResult> ReviewReference(Guid id, long version, string evidence, string nextStep, string commandId, string? returnUrl, CancellationToken c) {
        var o = organizationProvider.GetCurrent().RequireOrganizationId();
        var back = SafeReturnUrl(returnUrl, "/Evolution/Cycles", "/Evolution/", "/Diagnostics/", "/ActionCenter/", "/Results/");
        try {
            await cycles.ReviewReference(o, UserId, id, version, evidence, nextStep, commandId, c);
            TempData["EvolutionSuccess"] = "Revisão registrada. A referência permanece pendente de comprovação histórica.";
            return RedirectToAction(nameof(Details), new { id, returnUrl = back });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException) {
            ModelState.AddModelError("", error.Message);
            ViewData["ReviewEvidence"] = evidence; ViewData["ReviewNextStep"] = nextStep;
            var cycle = await cycles.Get(o, id, c);
            return cycle is null ? NotFound() : View("Details", await DetailsModel(o, cycle, back, c));
        }
    }

    private async Task<EvolutionDetailsViewModel> DetailsModel(Guid o, EvolutionCycleDto cycle, string back, CancellationToken c) {
        await LoadTimeZone(o, c);
        var options = (await cycles.ResultOptions(o, null, c)).ToList();
        if (ViewData["SelectedSurveyId"] is Guid selected && options.All(x => x.SourceSurveyId != selected) && await cycles.SelectedOption(o, selected, c) is { } option) options.Add(option);
        return new(cycle, await snapshots.List(o, cycle.Id, c), back) {
            Baseline = await cycles.Baseline(o, cycle.Id, c),
            Version = await cycles.Version(o, cycle.Id, c),
            Options = options
        };
    }
    private async Task<TimeZoneInfo> LoadTimeZone(Guid o, CancellationToken c) {
        var name = (await organizations.GetAsync(o, c))?.TimeZone ?? "UTC";
        ViewData["OrganizationTimeZone"] = name;
        return TimeZoneInfo.FindSystemTimeZoneById(name);
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/{id:guid}/Preview"), Authorize(Policy = ValoraPermissions.Evolution.Manage)]
    public async Task<IActionResult> Preview(Guid id, Guid sourceSurveyId, string? returnUrl, CancellationToken c) {
        var o = organizationProvider.GetCurrent().RequireOrganizationId();
        var cycle = await cycles.Get(o, id, c);
        if (cycle is null) return NotFound();
        ViewData["SelectedSurveyId"] = sourceSurveyId;
        try { ViewData["ComparisonPreview"] = await cycles.Preview(o, UserId, id, sourceSurveyId, c); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException) { ModelState.AddModelError("SourceSurveyId", error.Message); }
        return View("Details", await DetailsModel(o, cycle, SafeReturnUrl(returnUrl, "/Evolution/Cycles", "/Evolution/", "/Diagnostics/", "/ActionCenter/", "/Results/"), c));
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/{id:guid}/Reevaluate"), Authorize(Policy = ValoraPermissions.Evolution.Manage)]
    public async Task<IActionResult> Reevaluate(Guid id, Guid sourceSurveyId, long version, string fingerprint, string commandId, string? returnUrl, CancellationToken c) {
        var o = organizationProvider.GetCurrent().RequireOrganizationId();
        try {
            await cycles.ConfirmReevaluation(o, UserId, id, sourceSurveyId, version, fingerprint, commandId, c);
            TempData["EvolutionSuccess"] = "Reavaliação preservada no histórico.";
            return RedirectToAction(nameof(Details), new { id, returnUrl });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException) {
            ModelState.AddModelError("SourceSurveyId", error.Message);
            var cycle = await cycles.Get(o, id, c);
            if (cycle is null) return NotFound();
            ViewData["SelectedSurveyId"] = sourceSurveyId;
            return View("Details", await DetailsModel(o, cycle, SafeReturnUrl(returnUrl, "/Evolution/Cycles", "/Evolution/", "/Diagnostics/", "/ActionCenter/", "/Results/"), c));
        }
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/{id:guid}/Close"), Authorize(Policy = ValoraPermissions.Evolution.Manage)]
    public async Task<IActionResult> Close(Guid id, long version, string learning, string pendingTreatment, string? returnUrl, CancellationToken c) {
        var o = organizationProvider.GetCurrent().RequireOrganizationId();
        try {
            await cycles.CloseReviewed(o, UserId, id, version, learning, pendingTreatment, c);
            TempData["EvolutionSuccess"] = "Ciclo encerrado. Os planos e as atividades mantêm suas situações.";
            return RedirectToAction(nameof(Details), new { id, returnUrl });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException) {
            ModelState.AddModelError("", error.Message);
            ViewData["Learning"] = learning; ViewData["PendingTreatment"] = pendingTreatment;
            var cycle = await cycles.Get(o, id, c);
            return cycle is null ? NotFound() : View("Details", await DetailsModel(o, cycle, SafeReturnUrl(returnUrl, "/Evolution/Cycles", "/Evolution/", "/Diagnostics/", "/ActionCenter/", "/Results/"), c));
        }
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails {
        Title = "Organização não selecionada",
        Detail = CurrentOrganizationContext.RequiredMessage,
        Status = StatusCodes.Status403Forbidden
    });
}

public sealed record EvolutionDetailsViewModel(EvolutionCycleDto Cycle, IReadOnlyList<EvolutionSnapshotDto> Snapshots, string ReturnUrl) {
    public PreservedEvolutionMeasurement? Baseline { get; init; }
    public long Version { get; init; }
    public IReadOnlyList<EvolutionResultOptionDto> Options { get; init; } = [];
}
