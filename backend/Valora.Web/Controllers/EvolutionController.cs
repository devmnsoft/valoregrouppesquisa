using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Access;
using Valora.Application.Evolution;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize(Policy=ValoraPermissions.Evolution.Read)]
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
    public async Task<IActionResult> Cycles(CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        return organization.IsResolved
            ? View(await cycles.List(organization.RequireOrganizationId(), cancellationToken))
            : OrganizationRequired();
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/Open"),Authorize(Policy=ValoraPermissions.Evolution.Manage)]
    public async Task<IActionResult> Open(EvolutionCycleViewModel model, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        if (model.PeriodEnd.HasValue && model.PeriodEnd.Value < model.PeriodStart)
            ModelState.AddModelError(nameof(model.PeriodEnd), "A data final deve ser posterior à data inicial.");
        if (!ModelState.IsValid) {
            TempData["EvolutionError"] = "Revise os campos destacados antes de continuar.";
            ViewData["CycleCommand"] = model;
            return View("Cycles", await cycles.List(organization.RequireOrganizationId(), cancellationToken));
        }
        var request = new OpenEvolutionCycleRequest(null, null, null, model.Title, model.Summary,
            model.BaselineScore, model.TargetScore, null, model.PeriodStart, model.PeriodEnd, model.EvidenceSummary);
        Guid id;
        try { id = await cycles.Open(organization.RequireOrganizationId(), UserId, request, cancellationToken); }
        catch(ArgumentException error) { ModelState.AddModelError("",error.Message);TempData["EvolutionError"]="Revise os campos destacados antes de continuar.";ViewData["CycleCommand"]=model;return View("Cycles",await cycles.List(organization.RequireOrganizationId(),cancellationToken)); }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("Cycles/Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        var cycle = await cycles.Get(organization.RequireOrganizationId(), id, cancellationToken);
        return cycle is null
            ? NotFound()
            : View(new EvolutionDetailsViewModel(cycle, await snapshots.List(organization.RequireOrganizationId(), id, cancellationToken)));
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/{id:guid}/Snapshot"),Authorize(Policy=ValoraPermissions.Evolution.SnapshotsGenerate)]
    public async Task<IActionResult> Snapshot(Guid id, EvolutionSnapshotViewModel model, CancellationToken cancellationToken) {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        if (!ModelState.IsValid) {
            var cycle=await cycles.Get(organization.RequireOrganizationId(),id,cancellationToken);
            if(cycle is null)return NotFound();
            ViewData["SnapshotCommand"]=model;
            return View("Details",new EvolutionDetailsViewModel(cycle,await snapshots.List(organization.RequireOrganizationId(),id,cancellationToken)));
        }
        try { await snapshots.Generate(organization.RequireOrganizationId(), UserId, id, model.Evidence, model.Interpretation, model.Recommendation, cancellationToken); }
        catch(KeyNotFoundException){return NotFound();}
        catch(InvalidOperationException error){ModelState.AddModelError("",error.Message);var cycle=await cycles.Get(organization.RequireOrganizationId(),id,cancellationToken);if(cycle is null)return NotFound();ViewData["SnapshotCommand"]=model;return View("Details",new EvolutionDetailsViewModel(cycle,await snapshots.List(organization.RequireOrganizationId(),id,cancellationToken)));}
        TempData["EvolutionSuccess"]="Leitura preservada. Execução e mudança observada permanecem separadas.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails {
        Title = "Organização não selecionada",
        Detail = CurrentOrganizationContext.RequiredMessage,
        Status = StatusCodes.Status403Forbidden
    });
}

public sealed record EvolutionDetailsViewModel(EvolutionCycleDto Cycle, IReadOnlyList<EvolutionSnapshotDto> Snapshots);
