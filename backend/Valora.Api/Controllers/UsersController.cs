using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Valora.Application.Contracts;using Valora.Application.DTOs;
namespace Valora.Api.Controllers;
[Authorize,ApiController,Route("api/v1/users")]
public sealed class UsersController(IUserAdministrationService service, ISessionRepository sessions):ControllerBase
{
 [HttpGet]public async Task<IActionResult> List([FromQuery]UserListQuery query,CancellationToken ct)=>Ok(await service.ListAsync(Tenant(),query,ct));
 [HttpGet("{id:guid}")]public async Task<IActionResult> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(Tenant(),id,ct));
 [HttpPut("{id:guid}")]public async Task<IActionResult> Put(Guid id,UpdateUserRequest request,CancellationToken ct)=>Ok(await service.UpdateAsync(Tenant(),id,request,ct));
 [HttpPatch("{id:guid}")]public async Task<IActionResult> Patch(Guid id,UpdateUserRequest request,CancellationToken ct)=>Ok(await service.UpdateAsync(Tenant(),id,request,ct));
 [HttpPatch("{id:guid}/status")]public async Task<IActionResult> Status(Guid id,UpdateUserStatusRequest request,CancellationToken ct){await service.UpdateStatusAsync(Tenant(),Actor(),id,request,ct);return NoContent();}
 [HttpPost("{id:guid}/suspend")]public Task<IActionResult> Suspend(Guid id,CancellationToken ct)=>ChangeStatus(id,"suspended",ct);
 [HttpPost("{id:guid}/activate")]public Task<IActionResult> Activate(Guid id,CancellationToken ct)=>ChangeStatus(id,"active",ct);
 [HttpPost("{id:guid}/revoke-sessions")]public async Task<IActionResult> RevokeSessions(Guid id,CancellationToken ct){await service.GetAsync(Tenant(),id,ct);await sessions.RevokeAllAsync(id,"administrative_revocation");return NoContent();}
 [HttpPut("{id:guid}/roles")]public async Task<IActionResult> Roles(Guid id,UpdateUserRolesRequest request,CancellationToken ct){await service.SetRolesAsync(Tenant(),Actor(),id,request,ct);return NoContent();}
 [HttpPut("{id:guid}/scopes")]public async Task<IActionResult> Scopes(Guid id,UpdateUserScopesRequest request,CancellationToken ct){await service.SetScopesAsync(Tenant(),Actor(),id,request,ct);return NoContent();}
 private async Task<IActionResult> ChangeStatus(Guid id,string status,CancellationToken ct){await service.UpdateStatusAsync(Tenant(),Actor(),id,new UpdateUserStatusRequest(status),ct);if(status=="suspended")await sessions.RevokeAllAsync(id,"user_suspended");return NoContent();}
 private Guid Tenant()=>Guid.TryParse(User.FindFirstValue("organization_id"),out var id)?id:Guid.Empty;private Guid Actor()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
}
