using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.CommercialDelivery;
using Valora.Application.Access;
using Valora.Application.Common;

namespace Valora.Api.Controllers;

[Authorize, ApiController]
[Route("api/v1/diagnostics/{id:guid}/campaign")]
[Route("api/v1/diagnostics/{id:guid}/campaigns")]
public sealed class DiagnosticCampaignsController(IDiagnosticCampaignService campaigns, ICurrentRequestContext currentRequest) : ControllerBase
{
    [HttpGet("/api/v1/diagnostic-campaigns")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Read)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var access = Access();
        return access.Error ?? Ok(await campaigns.ListAsync(access.OrganizationId, ct));
    }

    [HttpGet]
    [Authorize(Policy = ValoraPermissions.Campaigns.Read)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var access = Access();
        if (access.Error is not null) return access.Error;
        var campaign = await campaigns.GetAsync(access.OrganizationId, id, ct);
        return campaign is null ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Nenhuma campanha foi criada para este diagnóstico.")) : Ok(campaign);
    }

    [HttpPost]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public async Task<IActionResult> Create(Guid id, [FromBody] CreateCampaignRequest request, CancellationToken ct)
    {
        var access = Access();
        if (access.Error is not null) return access.Error;
        try
        {
            var campaign = await campaigns.CreateAsync(access.OrganizationId, id, UserId, request, HttpContext.TraceIdentifier, ct);
            return campaign is null ? NotFound(Error("DIAGNOSTIC_NOT_FOUND", "Diagnóstico não encontrado.")) : Created($"/api/v1/diagnostics/{id}/campaign", campaign);
        }
        catch (InvalidOperationException exception)
        {
            return UnprocessableEntity(Error("CAMPAIGN_NOT_AVAILABLE", exception.Message));
        }
    }

    [HttpPost("schedule")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public Task<IActionResult> Schedule(Guid id, CampaignTransitionRequest request, CancellationToken ct) => Transition(id, DiagnosticCampaignStatus.Scheduled, request, ct);

    [HttpPost("send")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public Task<IActionResult> Send(Guid id, CampaignTransitionRequest request, CancellationToken ct) => Transition(id, DiagnosticCampaignStatus.Sending, request, ct);

    [HttpPost("pause")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public Task<IActionResult> Pause(Guid id, CampaignTransitionRequest request, CancellationToken ct) => Transition(id, DiagnosticCampaignStatus.Paused, request, ct);

    [HttpPost("resume")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public Task<IActionResult> Resume(Guid id, CampaignTransitionRequest request, CancellationToken ct) => Transition(id, DiagnosticCampaignStatus.Active, request, ct);

    [HttpPost("close")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public Task<IActionResult> Close(Guid id, CampaignTransitionRequest request, CancellationToken ct) => Transition(id, DiagnosticCampaignStatus.Closed, request, ct);

    [HttpPost("cancel")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public Task<IActionResult> Cancel(Guid id, CampaignTransitionRequest request, CancellationToken ct) => Transition(id, DiagnosticCampaignStatus.Cancelled, request, ct);

    [HttpGet("recipients")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Read)]
    public async Task<IActionResult> Recipients(Guid id, CancellationToken ct)
    {
        var access = Access(); if (access.Error is not null) return access.Error;
        var campaign = await campaigns.GetAsync(access.OrganizationId, id, ct);
        return campaign is null ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Campanha não encontrada.")) : Ok(campaign.Recipients);
    }

    [HttpGet("metrics")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Read)]
    public async Task<IActionResult> Metrics(Guid id, CancellationToken ct)
    {
        var access = Access(); if (access.Error is not null) return access.Error;
        var campaign = await campaigns.GetAsync(access.OrganizationId, id, ct);
        return campaign is null ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Campanha não encontrada.")) : Ok(new CampaignMetricsDto(
            campaign.RecipientCount, campaign.QueuedCount, campaign.SentCount, campaign.OpenedCount,
            campaign.StartedCount, campaign.CompletedCount, campaign.ExpiredCount, campaign.FailedCount,
            campaign.CompletionRate, DateTimeOffset.UtcNow));
    }

    [HttpGet("history")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Read)]
    public async Task<IActionResult> History(Guid id, CancellationToken ct)
    {
        var access = Access(); if (access.Error is not null) return access.Error;
        return Ok(await campaigns.HistoryAsync(access.OrganizationId, id, ct));
    }

    [HttpPost("resend-failures")]
    [Authorize(Policy = ValoraPermissions.Campaigns.Manage)]
    public async Task<IActionResult> ResendFailures(Guid id, CancellationToken ct) =>
        await Command(id, ValoraPermissions.Campaigns.Manage, organizationId =>
            campaigns.ResendFailuresAsync(organizationId, id, UserId, HttpContext.TraceIdentifier, ct));

    private async Task<IActionResult> Transition(Guid id, string status, CampaignTransitionRequest request, CancellationToken ct)
    {
        try
        {
            return await Command(id, ValoraPermissions.Campaigns.Manage, organizationId =>
                campaigns.TransitionAsync(organizationId, id, UserId, status, request, HttpContext.TraceIdentifier, ct));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(Error("CAMPAIGN_TRANSITION_INVALID", exception.Message));
        }
    }

    private async Task<IActionResult> Command(Guid id, string permission, Func<Guid, Task<CampaignCommandResult?>> command)
    {
        var access = Access();
        if (access.Error is not null) return access.Error;
        var result = await command(access.OrganizationId);
        return result is null ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Nenhuma campanha foi criada para este diagnóstico.")) : Ok(result);
    }

    private (Guid OrganizationId, IActionResult? Error) Access()
    {
        var organizationId = currentRequest.GetCurrent().EffectiveOrganizationId;
        if (organizationId is null || organizationId == Guid.Empty)
            return (Guid.Empty, StatusCode(403, Error("ORGANIZATION_SCOPE_REQUIRED", "Selecione uma organização para acessar este recurso.")));
        return (organizationId.Value, null);
    }

    private object Error(string code, string message) => new { code, message, correlationId = HttpContext.TraceIdentifier };
    private Guid UserId => currentRequest.GetCurrent().RequireUserId();
}
