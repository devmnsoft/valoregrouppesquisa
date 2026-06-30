using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;

namespace ValoraPesquisa.Api.Controllers;
[ApiController,Authorize,Route("forms")] public sealed class FormsController(IFormRepository repo,IAuditService audit):ApiBase{ [HttpGet] public Task<IReadOnlyList<FormDto>> List()=>repo.ListAsync(Current()); [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id)=>await repo.GetAsync(id,Current()) is { } f?Ok(f):NotFound(); [HttpPost] public Task<IActionResult> Post(FormInput i)=>Save(Guid.NewGuid(),i); [HttpPut("{id:guid}")] public Task<IActionResult> Put(Guid id,FormInput i)=>Save(id,i); async Task<IActionResult> Save(Guid id,FormInput i){var org=OrgOrThrow(Current(),i.OrganizationId); var qs=i.Questions.Select((q,idx)=>new Question(q.Id??Guid.NewGuid(),id,q.Text,q.Type,idx+1,q.Required,q.Weight,q.MaxScore,q.Options.Select((o,j)=>new QuestionOption(o.Id??Guid.NewGuid(),q.Id??Guid.Empty,o.Text,o.Score,j+1)).ToList())).ToList(); qs=qs.Select(q=>q with {Options=q.Options.Select(o=>o with {QuestionId=q.Id}).ToList()}).ToList(); var saved=await repo.SaveAsync(new Form(id,org,i.Title,i.Description,i.Status??"active",DateTimeOffset.UtcNow,null,false,qs),Current()); await audit.RecordAsync(org,Current().Id,"form_saved","form",id,Cid,new{saved.Title}); return Ok(saved);} }
public record FormInput(Guid? OrganizationId,string Title,string? Description,string? Status,IReadOnlyList<QuestionInput> Questions); public record QuestionInput(Guid? Id,string Text,string Type,bool Required,decimal Weight,decimal MaxScore,IReadOnlyList<OptionInput> Options); public record OptionInput(Guid? Id,string Text,decimal Score);
