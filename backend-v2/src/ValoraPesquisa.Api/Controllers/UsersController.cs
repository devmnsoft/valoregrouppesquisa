using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;

namespace ValoraPesquisa.Api.Controllers;
[ApiController,Authorize,Route("users")] public sealed class UsersController(IUserRepository repo,IPasswordHasher hasher,IAuditService audit):ApiBase{
 static readonly string[] AllowedRoles={ValoraPesquisa.Domain.Enums.Roles.AdminValora,ValoraPesquisa.Domain.Enums.Roles.EmpresaAdmin,ValoraPesquisa.Domain.Enums.Roles.GestorPesquisa,ValoraPesquisa.Domain.Enums.Roles.Participante};
 [HttpGet] public Task<IReadOnlyList<UserDto>> Get()=>repo.ListAsync(Current()); [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id){var u=await repo.FindByIdAsync(id); if(u==null)return NotFound(); if(u.OrganizationId.HasValue) ValoraPesquisa.Infrastructure.Repositories.Scope.DemandOrg(Current(),u.OrganizationId.Value); return Ok(new UserDto(u.Id,u.OrganizationId,u.Name,u.Email,u.Role,u.Status,u.Phone,u.LastLoginAt,u.CreatedAt,u.UpdatedAt));}
 [HttpPost] public async Task<IActionResult> Post(UserInput i){ var cur=Current(); if(!AllowedRoles.Contains(i.Role)) throw new ArgumentException("Perfil inválido."); var org=cur.IsAdminValora?i.OrganizationId:cur.OrganizationId; if(!cur.IsAdminValora && i.Role==Roles.AdminValora) return Forbid(); var u=new User(Guid.NewGuid(),org,i.Name,i.Email,hasher.Hash(i.Password),i.Role,"active",i.Phone,null,DateTimeOffset.UtcNow,null,false); var saved=await repo.CreateAsync(u); await audit.RecordAsync(saved.OrganizationId,cur.Id,"user_created","user",saved.Id,Cid,new{saved.Email,saved.Role}); return Ok(saved);}
 [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id,UserUpdateInput i){var saved=await repo.UpdateAsync(id,i.Name,i.Email,i.Phone,i.Role,i.OrganizationId,Current()); await audit.RecordAsync(saved.OrganizationId,Current().Id,"user_updated","user",id,Cid,new{saved.Email,saved.Role}); return Ok(saved);} [HttpPatch("{id:guid}/status")] public async Task<IActionResult> Status(Guid id,StatusInput i){await repo.SetStatusAsync(id,i.Status,Current()); await audit.RecordAsync(Current().OrganizationId,Current().Id,"user_status_changed","user",id,Cid,new{i.Status}); return Ok(new{ok=true,id,status=i.Status});}}
public record UserInput(Guid? OrganizationId,string Name,string Email,string Password,string Role,string? Phone); public record UserUpdateInput(Guid? OrganizationId,string Name,string Email,string Role,string? Phone); public record StatusInput(string Status);
