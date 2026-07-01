using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
namespace Valora.Api.Controllers;
[Authorize][ApiController]
public sealed class ReportsController(IReportService reports):ControllerBase{ Guid Org=>Guid.Parse(User.FindFirstValue("organization_id")??Guid.Empty.ToString()); Guid? UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null; IActionResult Safe(Func<Task<object?>> f){try{return Ok(f().GetAwaiter().GetResult());}catch(InvalidOperationException ex){return StatusCode(403,new{ok=false,code=ex.Message,correlationId=HttpContext.TraceIdentifier});}}
[HttpGet("/reports/surveys/{surveyId:guid}")] public IActionResult Survey(Guid surveyId)=>Safe(async()=>new{ok=true,report=await reports.GenerateSurveyAsync(Org,surveyId,"html",UserId)});
[HttpGet("/reports/responses/{responseId:guid}")] public IActionResult Response(Guid responseId)=>Safe(async()=>new{ok=true,report=await reports.GenerateResponseAsync(Org,responseId,"html",UserId)});
[HttpGet("/reports/organization")] public IActionResult Organization()=>Safe(async()=>new{ok=true,report=await reports.GenerateOrganizationAsync(Org,"html",UserId)});
[HttpPost("/reports/surveys/{surveyId:guid}/generate")] public IActionResult Generate(Guid surveyId,[FromBody]GenerateReportRequest req)=>Safe(async()=>new{ok=true,report=await reports.GenerateSurveyAsync(Org,surveyId,req.Format,UserId)});
[HttpGet("/reports/generated")] public async Task<IActionResult> Generated()=>Ok(new{ok=true,data=await reports.ListGeneratedAsync(Org)});
[HttpGet("/reports/generated/{id:guid}")] public async Task<IActionResult> Get(Guid id){var r=await reports.GetGeneratedAsync(Org,id); return r is null?NotFound(new{ok=false,code="NOT_FOUND"}):Ok(new{ok=true,report=r});}}
