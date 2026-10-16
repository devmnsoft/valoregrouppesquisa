using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Authorization;
using Valora.Application.Access;
using Valora.Application.Campaigns;
using Valora.Application.Common;

namespace Valora.Api.Controllers;

[Authorize, ApiController, RequireCommercialModule("surveys")]
[Route("api/v1/campaigns")]
public sealed class CampaignsController(
    ICampaignService campaigns,
    ICurrentRequestContext currentRequest) : ControllerBase {

    [HttpGet]
    [Authorize(Policy = ValoraPermissions.Campaigns.Read)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? surveyId,
        [FromQuery] Guid? unitId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        CancellationToken ct) {
        var access = Access();
        if (access.Error is not null) return access.Error;

        var filter = new CampaignFilter(surveyId, unitId, departmentId, status);
        var result = await campaigns.ListAsync(access.OrganizationId, filter, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Read)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) {
        var access = Access();
        if (access.Error is not null) return access.Error;

        var campaign = await campaigns.GetByIdAsync(access.OrganizationId, id, ct);
        return campaign is null
            ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Campanha não encontrada."))
            : Ok(campaign);
    }

    [HttpPost]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateCampaignDto request, CancellationToken ct) {
        var access = Access();
        if (access.Error is not null) return access.Error;

        try {
            var created = await campaigns.CreateAsync(access.OrganizationId, UserId, request, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException exception) {
            return BadRequest(Error("INVALID_ARGUMENT", exception.Message));
        }
        catch (InvalidOperationException exception) {
            return UnprocessableEntity(Error("CAMPAIGN_OPERATION_INVALID", exception.Message));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampaignDto request, CancellationToken ct) {
        var access = Access();
        if (access.Error is not null) return access.Error;

        try {
            var updated = await campaigns.UpdateAsync(access.OrganizationId, id, request, ct);
            return updated is null
                ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Campanha não encontrada."))
                : Ok(updated);
        }
        catch (ArgumentException exception) {
            return BadRequest(Error("INVALID_ARGUMENT", exception.Message));
        }
        catch (InvalidOperationException exception) {
            return Conflict(Error("CAMPAIGN_UPDATE_CONFLICT", exception.Message));
        }
    }

    [HttpPost("{id:guid}/{actionName}")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public async Task<IActionResult> Transition(Guid id, string actionName, [FromBody] CampaignTransitionDto? body, CancellationToken ct) {
        var access = Access();
        if (access.Error is not null) return access.Error;

        var targetStatus = actionName.ToLowerInvariant() switch {
            "schedule" => CanonicalCampaignStatus.Scheduled,
            "start" or "activate" => CanonicalCampaignStatus.Active,
            "pause" => CanonicalCampaignStatus.Paused,
            "resume" => CanonicalCampaignStatus.Active,
            "close" => CanonicalCampaignStatus.Closed,
            "cancel" => CanonicalCampaignStatus.Cancelled,
            _ => actionName.ToLowerInvariant()
        };

        try {
            var result = await campaigns.TransitionAsync(access.OrganizationId, id, targetStatus, body?.Justification, ct);
            return result is null
                ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Campanha não encontrada."))
                : Ok(result);
        }
        catch (ArgumentException exception) {
            return BadRequest(Error("INVALID_STATUS", exception.Message));
        }
        catch (InvalidOperationException exception) {
            return Conflict(Error("TRANSITION_NOT_ALLOWED", exception.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) {
        var access = Access();
        if (access.Error is not null) return access.Error;

        var deleted = await campaigns.DeleteAsync(access.OrganizationId, id, ct);
        return deleted ? NoContent() : NotFound(Error("CAMPAIGN_NOT_FOUND", "Campanha não encontrada."));
    }

    private (Guid OrganizationId, IActionResult? Error) Access() {
        var organizationId = currentRequest.GetCurrent().EffectiveOrganizationId;
        if (organizationId is null || organizationId == Guid.Empty)
            return (Guid.Empty, StatusCode(403, Error("ORGANIZATION_SCOPE_REQUIRED", "Selecione uma organização para acessar este recurso.")));
        return (organizationId.Value, null);
    }

    private object Error(string code, string message) =>
        new { code, message, correlationId = HttpContext.TraceIdentifier };

    private Guid UserId => currentRequest.GetCurrent().RequireUserId();
}
