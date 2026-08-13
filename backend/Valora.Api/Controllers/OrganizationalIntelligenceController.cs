using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/intelligence")]
public sealed class OrganizationalIntelligenceController(IOrganizationalIntelligenceService service, IOrganizationalIntelligencePipeline pipeline, IPermissionService permissions, IEntitlementService entitlements) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.read", (id) => service.DashboardAsync(id, ct));
    [HttpGet("runs")]
    public async Task<IActionResult> Runs([FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.read", (id) => service.RunsAsync(id, ct));
    [HttpGet("runs/{id:guid}")]
    public async Task<IActionResult> Run(Guid id, [FromQuery] Guid? organizationId, CancellationToken ct)
    {
        var denied = await Validate(organizationId, "organizational_intelligence.read"); if (denied.Error is not null) return denied.Error;
        var run = await service.RunAsync(denied.OrganizationId, id, ct); return run is null ? NotFound(new { code = "INTELLIGENCE_RUN_NOT_FOUND", message = "Leitura não encontrada." }) : Ok(run);
    }
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateOrganizationalIntelligenceRequest request, [FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.generate", (id) => service.GenerateAsync(id, ct));
    [HttpPost("pipeline/recalculate")]
    public async Task<IActionResult> Recalculate([FromQuery] Guid? organizationId, CancellationToken ct)
    {
        var access = await Validate(organizationId, "organizational_intelligence.generate"); if (access.Error is not null) return access.Error;
        return Ok(await pipeline.ProcessResponseAsync(new(access.OrganizationId, UserId: UserId, Trigger: "manual_recalculation"), ct));
    }
    [HttpGet("journey")]
    public async Task<IActionResult> Journey([FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.read", (id) => service.JourneyAsync(id, ct));
    [HttpPost("journey")]
    public async Task<IActionResult> CreateJourney([FromBody] CreateJourneyEventRequest request, [FromQuery] Guid? organizationId, CancellationToken ct)
    {
        var access = await Validate(organizationId, "organizational_intelligence.journey.create"); if (access.Error is not null) return access.Error;
        return StatusCode(201, await service.CreateJourneyAsync(access.OrganizationId, UserId, request, ct));
    }
    [HttpGet("indicators")]
    public async Task<IActionResult> Indicators([FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.read", _ => service.IndicatorsAsync(ct));
    [HttpGet("evolution")]
    public async Task<IActionResult> Evolution([FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.read", id => service.EvolutionAsync(id, ct));
    [HttpGet("heatmap")]
    public async Task<IActionResult> Heatmap([FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.read", async id => (await service.DashboardAsync(id, ct)).Evidence.Dimensions);
    [HttpGet("action-plans")]
    [HttpGet("actions")]
    public async Task<IActionResult> Actions([FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.read", id => service.ActionsAsync(id, ct));
    [HttpPost("action-plans")]
    [HttpPost("actions")]
    public async Task<IActionResult> CreateAction([FromBody] CreateValoraActionRequest request, [FromQuery] Guid? organizationId, CancellationToken ct)
    { var access = await Validate(organizationId, "organizational_intelligence.generate"); if (access.Error is not null) return access.Error; return StatusCode(201, await service.CreateActionAsync(access.OrganizationId, UserId, request, ct)); }
    [HttpPatch("action-plans/{id:guid}")]
    public async Task<IActionResult> UpdateAction(Guid id, [FromBody] UpdateValoraActionRequest request, [FromQuery] Guid? organizationId, CancellationToken ct)
    {
        var access = await Validate(organizationId, "organizational_intelligence.generate"); if (access.Error is not null) return access.Error;
        var action = await service.UpdateActionAsync(access.OrganizationId, id, UserId, request, ct);
        return action is null ? NotFound(new { code = "ACTION_PLAN_NOT_FOUND", message = "Plano de ação não encontrado." }) : Ok(action);
    }
    [HttpGet("action-plans/{id:guid}/history")]
    public async Task<IActionResult> ActionHistory(Guid id, [FromQuery] Guid? organizationId, CancellationToken ct) => await Read(organizationId, "organizational_intelligence.read", organization => service.ActionHistoryAsync(organization, id, ct));
    [HttpDelete("action-plans/{id:guid}")]
    public async Task<IActionResult> DeleteAction(Guid id, [FromQuery] Guid? organizationId, CancellationToken ct)
    {
        var access = await Validate(organizationId, "organizational_intelligence.generate"); if (access.Error is not null) return access.Error;
        return await service.DeleteActionAsync(access.OrganizationId, id, UserId, ct) ? Ok(new { archived = true }) : NotFound(new { code = "ACTION_PLAN_NOT_FOUND", message = "Plano de ação não encontrado." });
    }

    private async Task<IActionResult> Read<T>(Guid? requested, string permission, Func<Guid, Task<T>> action)
    { var access = await Validate(requested, permission); return access.Error ?? Ok(await action(access.OrganizationId)); }
    private async Task<(Guid OrganizationId, IActionResult? Error)> Validate(Guid? requested, string permission)
    {
        if (requested.HasValue && !CanSelectOrganization && requested != ClaimOrganizationId)
            return (Guid.Empty, Denied("ORGANIZATION_SCOPE_DENIED", "Seu perfil não pode selecionar outra organização."));
        var organizationId = CanSelectOrganization ? requested ?? ClaimOrganizationId : ClaimOrganizationId;
        if (organizationId is not Guid id) return (Guid.Empty, Denied("ORGANIZATION_REQUIRED", "Informe uma organização válida."));
        if (!IsAdmin && (!await permissions.HasPermissionAsync(UserId, permission, id) || !await entitlements.CanUseAsync(id, "organizational_intelligence")))
            return (id, Denied("PERMISSION_DENIED", "Seu perfil ou plano não possui acesso à Inteligência Organizacional."));
        return (id, null);
    }
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    private Guid? ClaimOrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : null;
    private bool CanSelectOrganization => HasRole("admin_valora") || HasRole("consultor_valora");
    private bool IsAdmin => HasRole("admin_valora");
    private bool HasRole(string role) => User.IsInRole(role) || User.Claims.Any(x => (x.Type is ClaimTypes.Role or "role") && string.Equals(x.Value, role, StringComparison.OrdinalIgnoreCase));
    private ObjectResult Denied(string code, string message) => StatusCode(403, new { code, message, correlationId = HttpContext.TraceIdentifier });
}
