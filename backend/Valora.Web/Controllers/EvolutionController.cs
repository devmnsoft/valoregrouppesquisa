using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.Common;
using Valora.Application.Evolution;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize(Policy = ValoraPermissions.Evolution.Read)]
[Route("Evolution")]
public sealed class EvolutionController(
    IEvolutionCycleService cycles,
    EvolutionSnapshotService snapshots,
    ICurrentOrganizationProvider organizationProvider) : Controller {
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        return organization.IsResolved
            ? View(await cycles.List(organization.RequireOrganizationId(), cancellationToken))
            : OrganizationRequired();
    }

    [HttpGet("Cycles")]
    public async Task<IActionResult> Cycles(Guid? diagnosticId, Guid? resultId, string? returnUrl, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        var command = ViewData["CycleCommand"] as EvolutionCycleViewModel ?? new EvolutionCycleViewModel {
            DiagnosticId = diagnosticId,
            ResultId = resultId,
            PeriodStart = DateTime.Today,
            EvidenceSummary = diagnosticId.HasValue ? "Baseline vinculado ao resultado selecionado do diagnóstico." : string.Empty
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
        var resultOptions = await cycles.ResultOptions(organization.RequireOrganizationId(), model.DiagnosticId, cancellationToken);
        var selectedResult = model.ResultId.HasValue ? resultOptions.FirstOrDefault(x => x.ResultId == model.ResultId.Value) : null;
        if (model.ResultId.HasValue && selectedResult is null)
            ModelState.AddModelError(nameof(model.ResultId), "Selecione um resultado calculado disponível para esta organização e diagnóstico.");
        if (model.PeriodEnd.HasValue && model.PeriodEnd.Value < model.PeriodStart)
            ModelState.AddModelError(nameof(model.PeriodEnd), "A data final deve ser posterior à data inicial.");
        if (!ModelState.IsValid) {
            TempData["EvolutionError"] = "Revise os campos destacados antes de continuar.";
            ViewData["CycleCommand"] = model;
            ViewData["ResultOptions"] = resultOptions;
            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl, "/Evolution", "/Diagnostics/", "/ActionCenter/", "/Results/");
            return View("Cycles", await cycles.List(organization.RequireOrganizationId(), cancellationToken));
        }
        var baseline = selectedResult?.Percentage ?? model.BaselineScore;
        var evidence = selectedResult is null
            ? model.EvidenceSummary
            : $"{model.EvidenceSummary.Trim()} Resultado calculado selecionado: {selectedResult.DiagnosticTitle}, {selectedResult.Percentage:0.0} pontos, metodologia {selectedResult.MethodologyVersion}, calculado em {selectedResult.CalculatedAt:dd/MM/yyyy}.";
        var request = new OpenEvolutionCycleRequest(selectedResult?.DiagnosticId ?? model.DiagnosticId, selectedResult?.ResultId ?? model.ResultId, null, model.Title, model.Summary,
            baseline, model.TargetScore, selectedResult is null ? null : "Resultado calculado", model.PeriodStart, model.PeriodEnd, evidence);
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
            : View(new EvolutionDetailsViewModel(cycle, await snapshots.List(organization.RequireOrganizationId(), id, cancellationToken), SafeReturnUrl(returnUrl, "/Evolution/Cycles", "/Diagnostics/", "/ActionCenter/", "/Results/")));
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/{id:guid}/Snapshot"), Authorize(Policy = ValoraPermissions.Evolution.SnapshotsGenerate)]
    public async Task<IActionResult> Snapshot(Guid id, EvolutionSnapshotViewModel model, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        if (!ModelState.IsValid) {
            var cycle = await cycles.Get(organization.RequireOrganizationId(), id, cancellationToken);
            if (cycle is null) return NotFound();
            ViewData["SnapshotCommand"] = model;
            return View("Details", new EvolutionDetailsViewModel(cycle, await snapshots.List(organization.RequireOrganizationId(), id, cancellationToken), "/Evolution/Cycles"));
        }
        try { await snapshots.Generate(organization.RequireOrganizationId(), UserId, id, model.Evidence, model.Interpretation, model.Recommendation, cancellationToken); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException error) { ModelState.AddModelError("", error.Message); var cycle = await cycles.Get(organization.RequireOrganizationId(), id, cancellationToken); if (cycle is null) return NotFound(); ViewData["SnapshotCommand"] = model; return View("Details", new EvolutionDetailsViewModel(cycle, await snapshots.List(organization.RequireOrganizationId(), id, cancellationToken), "/Evolution/Cycles")); }
        TempData["EvolutionSuccess"] = "Leitura preservada. Execução e mudança observada permanecem separadas.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static string SafeReturnUrl(string? value, string fallback, params string[] allowedPrefixes) {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("/", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal)) return fallback;
        return allowedPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ? value : fallback;
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails {
        Title = "Organização não selecionada",
        Detail = CurrentOrganizationContext.RequiredMessage,
        Status = StatusCodes.Status403Forbidden
    });
}

public sealed record EvolutionDetailsViewModel(EvolutionCycleDto Cycle, IReadOnlyList<EvolutionSnapshotDto> Snapshots, string ReturnUrl);
