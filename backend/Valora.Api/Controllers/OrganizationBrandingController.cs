using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize,ApiController]
public sealed class OrganizationBrandingController(IOrganizationBrandingService service) : ControllerBase
{
    [HttpGet("/api/v1/organization/current/branding")] public async Task<IActionResult> Get(CancellationToken ct)=>Ok(await service.GetAsync(Tenant(),ct));
    [HttpPut("/api/v1/organization/current/branding")] public async Task<IActionResult> Put(UpdateOrganizationBrandingRequest request,CancellationToken ct)=>Ok(await service.UpdateAsync(Tenant(),request,ct));
    [HttpGet("/api/v1/organization/current/subscription")] public async Task<IActionResult> Subscription(CancellationToken ct)=>Ok(await service.GetSubscriptionAsync(Tenant(),ct));
    [HttpGet("/api/v1/organization/current/onboarding")] public async Task<IActionResult> Onboarding(CancellationToken ct)=>Ok(await service.GetOnboardingAsync(Tenant(),ct));
    [HttpPost("/api/v1/organization/current/onboarding/{stepCode}/complete")] public async Task<IActionResult> Complete(string stepCode,CancellationToken ct){await service.CompleteStepAsync(Tenant(),stepCode,ct);return NoContent();}
    private Guid Tenant()=>Guid.TryParse(User.FindFirstValue("organization_id"),out var id)?id:Guid.Empty;
}
