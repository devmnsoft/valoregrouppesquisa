using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Valora.Application.Contracts;using Valora.Application.DTOs;
namespace Valora.Api.Controllers;
[Authorize,ApiController,Route("api/v1/users")]
public sealed class UsersController(IUserAdministrationService service):ControllerBase
{
 [HttpGet]public async Task<IActionResult> List([FromQuery]UserListQuery query,CancellationToken ct)=>Ok(await service.ListAsync(Tenant(),query,ct));
 [HttpGet("{id:guid}")]public async Task<IActionResult> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(Tenant(),id,ct));
 [HttpPut("{id:guid}")]public async Task<IActionResult> Put(Guid id,UpdateUserRequest request,CancellationToken ct)=>Ok(await service.UpdateAsync(Tenant(),id,request,ct));
 [HttpPatch("{id:guid}/status")]public async Task<IActionResult> Status(Guid id,UpdateUserStatusRequest request,CancellationToken ct){await service.UpdateStatusAsync(Tenant(),Actor(),id,request,ct);return NoContent();}
 [HttpPut("{id:guid}/roles")]public async Task<IActionResult> Roles(Guid id,UpdateUserRolesRequest request,CancellationToken ct){await service.SetRolesAsync(Tenant(),Actor(),id,request,ct);return NoContent();}
 [HttpPut("{id:guid}/scopes")]public async Task<IActionResult> Scopes(Guid id,UpdateUserScopesRequest request,CancellationToken ct){await service.SetScopesAsync(Tenant(),Actor(),id,request,ct);return NoContent();}
 private Guid Tenant()=>Guid.TryParse(User.FindFirstValue("organization_id"),out var id)?id:Guid.Empty;private Guid Actor()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
}
