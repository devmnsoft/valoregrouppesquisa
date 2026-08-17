using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1")]
public sealed class SaasAdministrationController(
    ISaasAdministrationRepository repository,
    IAccessAdministrationService access,
    IPlanRepository plans,
    IPlanEntitlementService entitlements,
    IAuditRepository audit,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("roles")]
    [Authorize(Policy = ValoraPermissions.Roles.Read)]
    public async Task<IActionResult> Roles(CancellationToken ct) => Ok(await access.ListRolesAsync(OrganizationId, ct));

    [HttpGet("permissions")]
    [Authorize(Policy = ValoraPermissions.Roles.Read)]
    public async Task<IActionResult> Permissions(CancellationToken ct) => Ok(await access.ListPermissionsAsync(ct));

    [HttpGet("roles/{id:guid}/permissions")]
    [Authorize(Policy = ValoraPermissions.Roles.Read)]
    public async Task<IActionResult> RolePermissions(Guid id, CancellationToken ct) => Ok(await access.GetRoleAsync(OrganizationId, id, ct));

    [HttpPatch("roles/{id:guid}/permissions")]
    [Authorize(Policy = ValoraPermissions.Roles.AssignPermissions)]
    public async Task<IActionResult> RolePermissions(Guid id, ReplaceRolePermissionsRequest request, CancellationToken ct) =>
        Ok(await access.ReplacePermissionsAsync(OrganizationId, UserId, id, request, ct));

    [HttpGet("plans/current")]
    public async Task<IActionResult> CurrentPlan()
    {
        var id = await plans.GetCurrentPlanIdAsync(OrganizationId) ?? "free";
        return Ok(await plans.GetByIdAsync(id));
    }

    [HttpGet("plans/features")]
    public async Task<IActionResult> PlanFeatures() => Ok((await CurrentPlanRecord())?.Capabilities ?? new Dictionary<string, string>());

    [HttpGet("plans/limits")]
    public async Task<IActionResult> PlanLimits() => Ok((await CurrentPlanRecord())?.Limits ?? new Dictionary<string, int>());

    [HttpGet("plans/usage")]
    public async Task<IActionResult> PlanUsage() => Ok(await entitlements.GetUsageAsync(OrganizationId));

    [HttpGet("audit")]
    [Authorize(Policy = ValoraPermissions.Audit.Read)]
    public async Task<IActionResult> Audit(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Ok(await audit.ListAdminAsync(OrganizationId));
    }

    [HttpGet("platform-governance")]
    public async Task<IActionResult> Governance([FromQuery] string? action, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct) =>
        Ok(await repository.ListGovernanceAsync(OrganizationId, IsPlatformAdmin, action, from, to, ct));

    [HttpGet("platform-governance/{id:guid}")]
    public async Task<IActionResult> Governance(Guid id, CancellationToken ct)
    {
        var item = await repository.GetGovernanceAsync(OrganizationId, IsPlatformAdmin, id, ct);
        return item is null ? NotFound(new { message = "Evento de governança não encontrado." }) : Ok(item);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications([FromQuery] string? type, [FromQuery] bool? unread, CancellationToken ct) =>
        Ok(await repository.ListNotificationsAsync(OrganizationId, UserId, type, unread, ct));

    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> ReadNotification(Guid id, CancellationToken ct) =>
        await repository.MarkNotificationReadAsync(OrganizationId, UserId, id, ct)
            ? NoContent()
            : NotFound(new { message = "Notificação não encontrada." });

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> ReadAllNotifications(CancellationToken ct) =>
        Ok(new { updated = await repository.MarkAllNotificationsReadAsync(OrganizationId, UserId, ct) });

    [HttpGet("system-health")]
    public async Task<IActionResult> SystemHealth(CancellationToken ct)
    {
        IReadOnlyList<SaasHealthEvent> events = IsPlatformAdmin
            ? await repository.ListHealthEventsAsync(ct)
            : Array.Empty<SaasHealthEvent>();
        return Ok(new
        {
            status = "operational",
            api = "operational",
            web = "operational",
            environment = environment.EnvironmentName,
            version = typeof(SaasAdministrationController).Assembly.GetName().Version?.ToString(),
            details = events
        });
    }

    private async Task<PlanDto?> CurrentPlanRecord()
    {
        var id = await plans.GetCurrentPlanIdAsync(OrganizationId) ?? "free";
        return await plans.GetByIdAsync(id);
    }

    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id"), out var id) ? id : Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    private bool IsPlatformAdmin => User.IsInRole("platform_admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "platform_admin");
}
