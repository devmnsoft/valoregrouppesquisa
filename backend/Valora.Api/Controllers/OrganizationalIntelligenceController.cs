using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/intelligence")]
public sealed class OrganizationalIntelligenceController(OrganizationalIntelligenceService service, IPermissionService permissions, IEntitlementService entitlements) : ControllerBase
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

    private async Task<IActionResult> Read<T>(Guid? requested, string permission, Func<Guid, Task<T>> action)
    { var access = await Validate(requested, permission); return access.Error ?? Ok(await action(access.OrganizationId)); }
    private async Task<(Guid OrganizationId, IActionResult? Error)> Validate(Guid? requested, string permission)
    {
        var organizationId = IsAdmin ? requested ?? ClaimOrganizationId : ClaimOrganizationId;
        if (organizationId is not Guid id) return (Guid.Empty, Denied("ORGANIZATION_REQUIRED", "Informe uma organização válida."));
        if (!IsAdmin && (!await permissions.HasPermissionAsync(UserId, permission, id) || !await entitlements.CanUseAsync(id, "organizational_intelligence")))
            return (id, Denied("PERMISSION_DENIED", "Seu perfil ou plano não possui acesso à Inteligência Organizacional."));
        return (id, null);
    }
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    private Guid? ClaimOrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : null;
    private bool IsAdmin => User.IsInRole("admin_valora") || User.Claims.Any(x => (x.Type is ClaimTypes.Role or "role") && x.Value == "admin_valora");
    private ObjectResult Denied(string code, string message) => StatusCode(403, new { code, message, correlationId = HttpContext.TraceIdentifier });
}
