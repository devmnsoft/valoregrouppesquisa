using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;

namespace ValoraPesquisa.Api.Controllers;
[ApiController,Authorize,Route("surveys")] public sealed class SurveysController(ISurveyRepository repo,IFormRepository forms,ISurveyLinkRepository links,ITokenHasher tokens,IAuditService audit):ApiBase{
 [HttpGet] public Task<IReadOnlyList<SurveyDto>> List()=>repo.ListAsync(Current()); [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id)=>await repo.GetAsync(id,Current()) is { } s?Ok(s):NotFound();
 [HttpPost] public Task<IActionResult> Post(SurveyInput i)=>Save(Guid.NewGuid(),i); [HttpPut("{id:guid}")] public Task<IActionResult> Put(Guid id,SurveyInput i)=>Save(id,i); async Task<IActionResult> Save(Guid id,SurveyInput i){var org=OrgOrThrow(Current(),i.OrganizationId); var s=new Survey(id,org,i.FormId,i.Title,i.Description,i.Status??SurveyStatuses.Draft,i.StartsAt,i.ExpiresAt,i.ShowResult,i.AllowRepeat,DateTimeOffset.UtcNow,null,false); var saved=await repo.SaveAsync(s,Current()); await audit.RecordAsync(org,Current().Id,"survey_saved","survey",id,Cid,new{saved.Title,saved.Status}); return Ok(saved);} 
 [HttpPatch("{id:guid}/status")] public async Task<IActionResult> Status(Guid id,StatusInput i){await repo.SetStatusAsync(id,i.Status,Current()); await audit.RecordAsync(Current().OrganizationId,Current().Id,"survey_status_changed","survey",id,Cid,new{i.Status}); return Ok(new{ok=true,id,status=i.Status});}
 [HttpGet("{surveyId:guid}/links")] public Task<IReadOnlyList<SurveyLinkDto>> Links(Guid surveyId)=>links.ListAsync(surveyId,Current());
 [HttpPost("{surveyId:guid}/links")] public async Task<IActionResult> CreateLink(Guid surveyId,CreateLinkInput i){var s=await repo.GetAsync(surveyId,Current())??throw new ArgumentException("Pesquisa não encontrada."); if(s.Status!=SurveyStatuses.Published) throw new ArgumentException("Link só pode ser criado para pesquisa publicada."); var token=tokens.NewToken(); var publicUrl=$"/Survey/Responder?surveyId={surveyId}&token={Uri.EscapeDataString(token)}&org={i.OrgSlug}"; var link=new SurveyLink(Guid.NewGuid(),surveyId,s.OrganizationId,tokens.Hash(token),publicUrl,"active",i.ExpiresAt,null,DateTimeOffset.UtcNow,null); var saved=await links.CreateAsync(link,token); await audit.RecordAsync(s.OrganizationId,Current().Id,"survey_link_created","survey_link",saved.Id,Cid,new{surveyId}); return Ok(saved);} }
public record SurveyInput(Guid? OrganizationId,Guid FormId,string Title,string? Description,string? Status,DateTimeOffset? StartsAt,DateTimeOffset? ExpiresAt,bool ShowResult,bool AllowRepeat); public record CreateLinkInput(string OrgSlug,DateTimeOffset? ExpiresAt);
