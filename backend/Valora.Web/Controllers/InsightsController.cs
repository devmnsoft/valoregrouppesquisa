using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.ValoraAi;

namespace Valora.Web.Controllers;

[Authorize]
[Route("Insights")]
public sealed class InsightsController(IValoraAiInsightRepository insights) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await insights.ListAsync(OrganizationId(), null, ct));

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var insight = await insights.GetAsync(OrganizationId(), id, ct);
        return insight is null ? NotFound() : View(insight);
    }

    private Guid OrganizationId() => Guid.TryParse(User.FindFirst("organization_id")?.Value, out var id) ? id : Guid.Empty;
}
