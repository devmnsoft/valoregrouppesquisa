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
    IOrganizationRepository organizations,
    PlanEntitlementService entitlements) : ControllerBase
{
    [HttpGet("/organizations/current")]
    public async Task<IActionResult> Current()
    {
        return Ok(await organizations.GetAsync(CurrentOrganizationId()));
    }

    [HttpPatch("/organizations/current")]
    public async Task<IActionResult> Patch(UpdateOrganizationRequest request)
    {
        await organizations.UpdateCurrentAsync(CurrentOrganizationId(), request);
        return Ok(await organizations.GetAsync(CurrentOrganizationId()));
    }

    [HttpGet("/organizations/current/usage")]
    public async Task<IActionResult> Usage()
    {
        return Ok(await entitlements.GetUsageAsync(CurrentOrganizationId()));
    }

    private Guid CurrentOrganizationId()
    {
        return Guid.Parse(User.FindFirstValue("organization_id")!);
    }
}
