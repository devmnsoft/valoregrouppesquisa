using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
public sealed class OrganizationsController(
    IOrganizationAdministrationService organizations,
    PlanEntitlementService entitlements) : ControllerBase
{
    [HttpGet("/api/v1/organization/current")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        return Ok(await organizations.GetCurrentAsync(CurrentOrganizationId(), cancellationToken));
    }

    [HttpPut("/api/v1/organization/current")]
    public async Task<IActionResult> Patch(UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await organizations.UpdateCurrentAsync(CurrentOrganizationId(), request, cancellationToken));
    }

    [HttpGet("/api/v1/organization/current/usage")]
    public async Task<IActionResult> Usage()
    {
        return Ok(await entitlements.GetUsageAsync(CurrentOrganizationId()));
    }

    private Guid CurrentOrganizationId()
    {
        return Guid.Parse(User.FindFirstValue("organization_id")!);
    }
}
