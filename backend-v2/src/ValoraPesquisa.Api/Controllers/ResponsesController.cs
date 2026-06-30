using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;

namespace ValoraPesquisa.Api.Controllers;
[ApiController,Authorize,Route("responses")] public sealed class ResponsesController(IResponseRepository repo):ApiBase{ [HttpGet] public Task<IReadOnlyList<ResponseDto>> List()=>repo.ListAsync(Current()); [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id)=>await repo.GetDetailAsync(id,Current()) is { } r?Ok(new{r.Id,r.OrganizationId,r.SurveyId,r.FormId,r.ParticipantName,r.ParticipantEmail,r.Status,r.CompletedAt,r.CreatedAt,r.Answers}):NotFound(); }
