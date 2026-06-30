using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;

namespace ValoraPesquisa.Api.Controllers;
[ApiController,Authorize,Route("organizations")] public sealed class OrganizationsController(IOrganizationRepository repo,IAuditService audit):ApiBase{
 [HttpGet] public Task<IReadOnlyList<OrganizationDto>> Get()=>repo.ListAsync(Current());
 [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id)=>await repo.FindByIdAsync(id,Current()) is { } o?Ok(o):NotFound();
 [HttpPost] public async Task<IActionResult> Post(OrganizationInput i){ if(!Current().IsAdminValora) return Forbid(); if(!i.Email.Contains('@')) throw new ArgumentException("E-mail inválido."); if(await repo.FindBySlugAsync(i.Slug)!=null) throw new ArgumentException("Slug já cadastrado."); var org=new Organization(Guid.NewGuid(),i.Name,i.PublicName,i.Slug,i.Document,i.Email,i.Phone,"active",i.PlanCode??"free",DateTimeOffset.UtcNow,null,Current().Id,null,false); var saved=await repo.CreateAsync(org); await audit.RecordAsync(saved.Id,Current().Id,"organization_created","organization",saved.Id,Cid,new{saved.Slug}); return Ok(saved);}
 [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id,OrganizationInput i){ var saved=await repo.UpdateAsync(id,i.Name,i.PublicName,i.Slug,i.Document,i.Email,i.Phone,"active",i.PlanCode??"free",Current()); await audit.RecordAsync(id,Current().Id,"organization_updated","organization",id,Cid,new{saved.Slug}); return Ok(saved);} }
public record OrganizationInput(string Name,string PublicName,string Slug,string? Document,string Email,string? Phone,string? PlanCode);
