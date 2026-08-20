using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/usage")]
public sealed class SubscriptionUsageController(ISubscriptionUsageService usage) : ControllerBase
{
    [HttpGet("current")]
    [Authorize(Policy = ValoraPermissions.Usage.Read)]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        if (OrganizationId == Guid.Empty) return Unauthorized();
        return Ok(await usage.GetCurrentAsync(OrganizationId, cancellationToken));
    }

    [HttpGet("limits/{metric}")]
    public async Task<IActionResult> Check(string metric, [FromQuery] int amount = 1, CancellationToken cancellationToken = default)
    {
        if (OrganizationId == Guid.Empty) return Unauthorized();
        var result = await usage.CheckAsync(OrganizationId, metric, amount, IsValoraAdmin, cancellationToken);
        return Ok(result); // A reached commercial limit is a business decision, not a technical HTTP error.
    }

    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : Guid.Empty;
    private bool IsValoraAdmin => User.IsInRole(ValoraAccessCatalog.PlatformRole) ||
        User.Claims.Any(claim => claim.Type == ClaimTypes.Role && claim.Value == ValoraAccessCatalog.PlatformRole);
}
