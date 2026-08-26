using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Evolution;

namespace Valora.Web.Controllers;

[Authorize]
[Route("Evolution")]
public sealed class EvolutionController(
    IEvolutionCycleService cycles,
    EvolutionSnapshotService snapshots,
    ICurrentOrganizationProvider organizationProvider) : Controller
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var organization = organizationProvider.GetCurrent();
        return organization.IsResolved
            ? View(await cycles.List(organization.OrganizationId, cancellationToken))
            : OrganizationRequired();
    }

    [HttpGet("Cycles")]
    public async Task<IActionResult> Cycles(CancellationToken cancellationToken)
    {
        var organization = organizationProvider.GetCurrent();
        return organization.IsResolved
            ? View(await cycles.List(organization.OrganizationId, cancellationToken))
            : OrganizationRequired();
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/Open")]
    public async Task<IActionResult> Open(OpenEvolutionCycleRequest request, CancellationToken cancellationToken)
    {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        var id = await cycles.Open(organization.OrganizationId, UserId, request, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("Cycles/Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        var cycle = await cycles.Get(organization.OrganizationId, id, cancellationToken);
        return cycle is null
            ? NotFound()
            : View(new EvolutionDetailsViewModel(cycle, await snapshots.List(organization.OrganizationId, id, cancellationToken)));
    }

    [ValidateAntiForgeryToken, HttpPost("Cycles/{id:guid}/Snapshot")]
    public async Task<IActionResult> Snapshot(Guid id, string evidence, string interpretation, string recommendation, CancellationToken cancellationToken)
    {
        var organization = organizationProvider.GetCurrent();
        if (!organization.IsResolved) return OrganizationRequired();
        await snapshots.Generate(organization.OrganizationId, UserId, id, evidence, interpretation, recommendation, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    private ObjectResult OrganizationRequired() => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
    {
        Title = "Organização não selecionada",
        Detail = CurrentOrganizationContext.RequiredMessage,
        Status = StatusCodes.Status403Forbidden
    });
}

public sealed record EvolutionDetailsViewModel(EvolutionCycleDto Cycle, IReadOnlyList<EvolutionSnapshotDto> Snapshots);
