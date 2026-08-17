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
    PlanEntitlementService entitlements,
    IOrganizationRepository repository) : ControllerBase
{
    [HttpGet("/api/v1/organization")]
    [HttpGet("/api/v1/organization/current")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        return Ok(await organizations.GetCurrentAsync(CurrentOrganizationId(), cancellationToken));
    }

    [HttpPatch("/api/v1/organization")]
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

    [HttpGet("/api/v1/organization/settings")]
    public async Task<IActionResult> Settings() => Ok(await repository.GetSettingsAsync(CurrentOrganizationId()));

    [HttpPatch("/api/v1/organization/settings")]
    public async Task<IActionResult> Settings([FromBody] Dictionary<string, object?> settings)
    {
        await repository.UpsertSettingsAsync(CurrentOrganizationId(), settings);
        return NoContent();
    }

    private Guid CurrentOrganizationId()
    {
        return Guid.Parse(User.FindFirstValue("organization_id")!);
    }
}
