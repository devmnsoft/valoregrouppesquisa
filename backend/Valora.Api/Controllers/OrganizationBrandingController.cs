using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize, ApiController]
public sealed class OrganizationBrandingController(
    IOrganizationBrandingService service,
    ICurrentOrganizationProvider organizationContext) : ControllerBase
{
    [HttpGet("/api/v1/organization/current/branding")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var context = organizationContext.GetCurrent();
        if (!context.IsResolved)
            return Ok(new
            {
                primaryColor = "#0B3B45",
                secondaryColor = "#2BB7A9",
                logoUrl = "/img/brand/valora-logo-full.svg",
                publicSlug = "valora",
                whiteLabelEnabled = false,
                version = 0,
                requiresOrganizationSelection = true,
                message = CurrentOrganizationContext.RequiredMessage
            });
        return Ok(await service.GetAsync(context.RequireOrganizationId(), ct));
    }

    [HttpPut("/api/v1/organization/current/branding")]
    public async Task<IActionResult> Put(UpdateOrganizationBrandingRequest request, CancellationToken ct)
    {
        if (!TryOrganization(out var id, out var error)) return error!;
        return Ok(await service.UpdateAsync(id, request, ct));
    }

    [HttpGet("/api/v1/organization/current/subscription")]
    public async Task<IActionResult> Subscription(CancellationToken ct)
    {
        if (!TryOrganization(out var id, out var error)) return error!;
        return Ok(await service.GetSubscriptionAsync(id, ct));
    }

    [HttpGet("/api/v1/organization/current/onboarding")]
    public async Task<IActionResult> Onboarding(CancellationToken ct)
    {
        if (!TryOrganization(out var id, out var error)) return error!;
        return Ok(await service.GetOnboardingAsync(id, ct));
    }

    [HttpPost("/api/v1/organization/current/onboarding/{stepCode}/complete")]
    public async Task<IActionResult> Complete(string stepCode, CancellationToken ct)
    {
        if (!TryOrganization(out var id, out var error)) return error!;
        await service.CompleteStepAsync(id, stepCode, ct);
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
        error = Problem(statusCode: StatusCodes.Status403Forbidden, title: "Organização necessária",
            detail: CurrentOrganizationContext.RequiredMessage, extensions: new Dictionary<string, object?>
            {
                ["correlationId"] = HttpContext.TraceIdentifier,
                ["traceId"] = HttpContext.TraceIdentifier,
                ["requiresOrganizationSelection"] = true
            });
        return false;
    }
}
