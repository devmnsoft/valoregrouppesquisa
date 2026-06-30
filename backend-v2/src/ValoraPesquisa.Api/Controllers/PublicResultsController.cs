using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Application.Services;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Domain.Enums;

namespace ValoraPesquisa.Api.Controllers;
[ApiController,Route("public/results")] public sealed class PublicResultsController(IResponseRepository repo,IAuditService audit):ControllerBase{ [HttpGet("{responseId:guid}")] public async Task<IActionResult> Result(Guid responseId,string token){var r=await repo.GetResultAsync(responseId,token); if(r==null)return Unauthorized(new{ok=false,code="INVALID_RESULT_TOKEN",message="Token de resultado inválido.",correlationId=HttpContext.Items["CorrelationId"]}); await audit.RecordAsync(null,null,"public_result_viewed","response",responseId,HttpContext.Items["CorrelationId"]?.ToString()??"",new{responseId}); return Ok(r);}}
