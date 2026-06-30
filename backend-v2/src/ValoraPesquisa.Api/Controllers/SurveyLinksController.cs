using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;

namespace ValoraPesquisa.Api.Controllers;
[ApiController,Authorize,Route("survey-links")] public sealed class SurveyLinksController(ISurveyLinkRepository repo,IAuditService audit):ApiBase{ [HttpPatch("{linkId:guid}/status")] public async Task<IActionResult> Status(Guid linkId,StatusInput i){await repo.SetStatusAsync(linkId,i.Status,Current()); await audit.RecordAsync(Current().OrganizationId,Current().Id,"survey_link_status_changed","survey_link",linkId,Cid,new{i.Status}); return Ok(new{ok=true,linkId,status=i.Status});}}
