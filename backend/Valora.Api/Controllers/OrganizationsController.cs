using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
public sealed class OrganizationsController(
    IOrganizationAdministrationService organizations,
    PlanEntitlementService entitlements,
    IOrganizationRepository repository,
    ICurrentOrganizationProvider organizationContext) : ControllerBase
{
    [HttpGet("/api/v1/organization")]
    [HttpGet("/api/v1/organization/current")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var context = organizationContext.GetCurrent();
        if (!context.IsResolved)
        {
            if (IsPlatformAdministrator())
                return Ok(new { requiresOrganizationSelection = true, message = CurrentOrganizationContext.RequiredMessage });

            return OrganizationRequired();
        }

        return Ok(await organizations.GetCurrentAsync(context.RequireOrganizationId(), cancellationToken));
    }

    [HttpPatch("/api/v1/organization")]
    [HttpPut("/api/v1/organization/current")]
    public async Task<IActionResult> Patch(UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        if (!TryOrganization(out var id, out var error)) return error!;
        return Ok(await organizations.UpdateCurrentAsync(id, request, cancellationToken));
    }

    [HttpGet("/api/v1/organization/current/usage")]
    public async Task<IActionResult> Usage()
    {
        if (!TryOrganization(out var id, out var error)) return error!;
        return Ok(await entitlements.GetUsageAsync(id));
    }

    [HttpGet("/api/v1/organization/settings")]
    public async Task<IActionResult> Settings()
    {
        if (!TryOrganization(out var id, out var error)) return error!;
        return Ok(await repository.GetSettingsAsync(id));
    }

    [HttpPatch("/api/v1/organization/settings")]
    public async Task<IActionResult> Settings([FromBody] Dictionary<string, object?> settings)
    {
        if (!TryOrganization(out var id, out var error)) return error!;
        await repository.UpsertSettingsAsync(id, settings);
        return NoContent();
    }

    private bool TryOrganization(out Guid id, out IActionResult? error)
    {
        var context = organizationContext.GetCurrent();
        if (context.OrganizationId is { } resolved)
        {
            id = resolved;
            error = null;
            return true;
        }

        id = default;
        error = OrganizationRequired();
        return false;
    }

    private ObjectResult OrganizationRequired() => Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Organização necessária",
        detail: CurrentOrganizationContext.RequiredMessage,
        extensions: new Dictionary<string, object?>
        {
            ["correlationId"] = HttpContext.TraceIdentifier,
            ["traceId"] = HttpContext.TraceIdentifier,
            ["requiresOrganizationSelection"] = true
        });

    private bool IsPlatformAdministrator() =>
        User.IsInRole("super_admin") || User.IsInRole("admin_valora") || User.IsInRole("platform_admin") || User.IsInRole("SuperAdmin");
}
