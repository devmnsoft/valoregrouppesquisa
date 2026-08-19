using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.CommercialDelivery;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[Authorize, ApiController]
[Route("api/v1/diagnostics/{id:guid}/campaign")]
[Route("api/v1/diagnostics/{id:guid}/campaigns")]
public sealed class DiagnosticCampaignsController(IDiagnosticCampaignService campaigns, IPermissionService permissions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var access = await AccessAsync("surveys.read");
        if (access.Error is not null) return access.Error;
        var campaign = await campaigns.GetAsync(access.OrganizationId, id, ct);
        return campaign is null ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Nenhuma campanha foi criada para este diagnóstico.")) : Ok(campaign);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid id, [FromBody] CreateCampaignRequest request, CancellationToken ct)
    {
        var access = await AccessAsync("surveys.distribute");
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

    [HttpPost("send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct) => await Command(id, "surveys.distribute", (organizationId) => campaigns.SendAsync(organizationId, id, UserId, HttpContext.TraceIdentifier, ct));

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => await Command(id, "surveys.distribute", (organizationId) => campaigns.CancelAsync(organizationId, id, UserId, HttpContext.TraceIdentifier, ct));

    private async Task<IActionResult> Command(Guid id, string permission, Func<Guid, Task<CampaignCommandResult?>> command)
    {
        var access = await AccessAsync(permission);
        if (access.Error is not null) return access.Error;
        var result = await command(access.OrganizationId);
        return result is null ? NotFound(Error("CAMPAIGN_NOT_FOUND", "Nenhuma campanha foi criada para este diagnóstico.")) : Ok(result);
    }

    private async Task<(Guid OrganizationId, IActionResult? Error)> AccessAsync(string permission)
    {
        if (!Guid.TryParse(User.FindFirstValue("organization_id"), out var organizationId))
            return (Guid.Empty, StatusCode(403, Error("ORGANIZATION_REQUIRED", "Selecione uma organização.")));
        if (!IsAdmin && !await permissions.HasPermissionAsync(UserId, permission, organizationId))
            return (organizationId, StatusCode(403, Error("CAMPAIGN_FORBIDDEN", "Você não possui permissão para gerenciar esta campanha.")));
        return (organizationId, null);
    }

    private object Error(string code, string message) => new { code, message, correlationId = HttpContext.TraceIdentifier };
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    private bool IsAdmin => User.IsInRole("admin_valora");
}
