using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/access")]
public sealed class AccessController(IAccessAdministrationService service) : ControllerBase
{
    [HttpGet("roles"), Authorize(Policy=ValoraPermissions.Roles.Read)] public async Task<IActionResult> Roles(CancellationToken ct)=>Ok(await service.ListRolesAsync(Tenant(),ct));
    [HttpGet("roles/{id:guid}"), Authorize(Policy=ValoraPermissions.Roles.Read)] public async Task<IActionResult> Role(Guid id,CancellationToken ct)=>Ok(await service.GetRoleAsync(Tenant(),id,ct));
    [HttpPost("roles"), Authorize(Policy=ValoraPermissions.Roles.Create)] public async Task<IActionResult> Create(CreateAccessRoleRequest request,CancellationToken ct){var role=await service.CreateRoleAsync(Tenant(),Actor(),request,ct);return CreatedAtAction(nameof(Role),new{id=role.Id},role);}
    [HttpPut("roles/{id:guid}"), Authorize(Policy=ValoraPermissions.Roles.Update)] public async Task<IActionResult> Update(Guid id,UpdateAccessRoleRequest request,CancellationToken ct)=>Ok(await service.UpdateRoleAsync(Tenant(),Actor(),id,request,ct));
    [HttpDelete("roles/{id:guid}"), Authorize(Policy=ValoraPermissions.Roles.Delete)] public async Task<IActionResult> Delete(Guid id,CancellationToken ct){await service.DeleteRoleAsync(Tenant(),Actor(),id,ct);return NoContent();}
    [HttpPut("roles/{id:guid}/permissions"), Authorize(Policy=ValoraPermissions.Roles.AssignPermissions)] public async Task<IActionResult> Permissions(Guid id,ReplaceRolePermissionsRequest request,CancellationToken ct)=>Ok(await service.ReplacePermissionsAsync(Tenant(),Actor(),id,request,ct));
    [HttpGet("permissions"), Authorize(Policy=ValoraPermissions.Roles.Read)] public async Task<IActionResult> PermissionCatalog(CancellationToken ct)=>Ok(await service.ListPermissionsAsync(ct));
    [HttpGet("modules"), Authorize(Policy=ValoraPermissions.Roles.Read)] public async Task<IActionResult> Modules(CancellationToken ct)=>Ok(await service.ListModulesAsync(ct));
    [HttpGet("users/{userId:guid}/effective-access"), Authorize(Policy=ValoraPermissions.Users.Read)] public async Task<IActionResult> Effective(Guid userId,CancellationToken ct)=>Ok(await service.GetEffectiveAccessAsync(Tenant(),userId,ct));
    private Guid Tenant()=>Guid.TryParse(User.FindFirstValue("organization_id") ?? User.FindFirstValue("organizationId"),out var id)?id:Guid.Empty;
    private Guid Actor()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
}
