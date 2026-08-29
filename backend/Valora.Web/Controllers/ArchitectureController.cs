using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.OrganizationalArchitecture;
using Valora.Web.Models.ViewModels;
namespace Valora.Web.Controllers;
[Authorize]
public sealed class ArchitectureController(ICurrentOrganizationProvider current,ArchitectureOverviewService overview,OrganizationUnitService units,OrganizationPositionService positions,ResponsibilityMatrixService responsibilities,BusinessProcessService processes,DecisionRightService decisions,OrganizationalDependencyService dependencies,ArchitectureRiskService risks,ArchitectureSnapshotService snapshots):Controller
{
 [HttpGet] public Task<IActionResult> Index(CancellationToken ct)=>Page("Overview",ct);
 [HttpGet] public Task<IActionResult> OrganizationMap(CancellationToken ct)=>Page("OrganizationMap",ct);
 [HttpGet] public Task<IActionResult> Responsibilities(CancellationToken ct)=>Page("Responsibilities",ct);
 [HttpGet] public Task<IActionResult> Processes(CancellationToken ct)=>Page("Processes",ct);
 [HttpGet] public Task<IActionResult> DecisionRights(CancellationToken ct)=>Page("DecisionRights",ct);
 [HttpGet] public Task<IActionResult> Dependencies(CancellationToken ct)=>Page("Dependencies",ct);
 [HttpGet] public Task<IActionResult> Risks(CancellationToken ct)=>Page("Risks",ct);
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Snapshot(CancellationToken ct){var o=RequireOrganization();await snapshots.CreateAsync(o,null,HttpContext.TraceIdentifier,ct);TempData["ArchitectureMessage"]="Snapshot criado. A memória organizacional foi preservada.";return RedirectToAction(nameof(Index));}
 private async Task<IActionResult>Page(string section,CancellationToken ct){var context=current.GetCurrent();if(!context.IsResolved)return View("MissingOrganization");var o=context.OrganizationId;var model=new ArchitectureStudioViewModel(section,await overview.GetAsync(o,ct),await units.ListAsync(o,ct),await positions.ListAsync(o,ct),await responsibilities.ListAsync(o,ct),await processes.ListAsync(o,ct),await decisions.ListAsync(o,ct),await dependencies.ListAsync(o,ct),await risks.ListAsync(o,ct),TempData["ArchitectureMessage"]?.ToString());return View("Studio",model);}
 private Guid RequireOrganization(){var x=current.GetCurrent();return x.IsResolved?x.OrganizationId:throw new UnauthorizedAccessException("Selecione uma organização para continuar.");}
}
