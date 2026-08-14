using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Api.Controllers;
[Authorize,ApiController,Route("api/v1/intelligence/processing")]
public sealed class IntelligenceProcessingController(IIntelligenceProcessingJobService jobs,IPermissionService permissions) : ControllerBase
{
    [HttpGet("jobs")] public async Task<IActionResult> List([FromQuery]Guid? organizationId,[FromQuery]string? status,[FromQuery]Guid? surveyId,[FromQuery]Guid? responseId,[FromQuery]string? trigger,[FromQuery]int page=1,[FromQuery]int pageSize=50,CancellationToken ct=default) => await WithAccess(organizationId, id=>jobs.ListJobsAsync(id,new(status,surveyId,responseId,trigger,page,pageSize),ct));
    [HttpGet("jobs/{id:guid}")] public async Task<IActionResult> Details(Guid id,[FromQuery]Guid? organizationId,CancellationToken ct) { var access=await Organization(organizationId); if(access.Error is not null)return access.Error; var value=await jobs.GetJobDetailsAsync(access.Id,id,ct); return value is null?NotFound(Error("PROCESSING_JOB_NOT_FOUND","Processamento não encontrado.")):Ok(value); }
    [HttpGet("jobs/{id:guid}/stages")] public async Task<IActionResult> Stages(Guid id,[FromQuery]Guid? organizationId,CancellationToken ct)=>await WithAccess(organizationId,o=>jobs.ListStageRunsAsync(o,id,ct));
    [HttpGet("summary")] public async Task<IActionResult> Summary([FromQuery]Guid? organizationId,CancellationToken ct)=>await WithAccess(organizationId,o=>jobs.GetSummaryAsync(o,ct));
    [HttpPost("jobs/{id:guid}/reprocess")] public async Task<IActionResult> Reprocess(Guid id,[FromQuery]Guid? organizationId,CancellationToken ct) { var a=await Organization(organizationId);if(a.Error is not null)return a.Error;var created=await jobs.ReprocessAsync(a.Id,id,UserId,HttpContext.TraceIdentifier,ct);return created is null?NotFound(Error("PROCESSING_JOB_NOT_FOUND","Processamento não encontrado.")):Accepted(new{id=created,status="pending",correlationId=HttpContext.TraceIdentifier}); }
    [HttpPost("jobs/{id:guid}/cancel")] public async Task<IActionResult> Cancel(Guid id,[FromQuery]Guid? organizationId,CancellationToken ct){var a=await Organization(organizationId);if(a.Error is not null)return a.Error;return await jobs.CancelAsync(a.Id,id,UserId,ct)?Ok(new{status="cancelled",correlationId=HttpContext.TraceIdentifier}):Conflict(Error("JOB_NOT_CANCELLABLE","Somente jobs pendentes ou em retry podem ser cancelados."));}
    [HttpPost("reprocess/diagnosis/{surveyId:guid}")] public async Task<IActionResult> Diagnosis(Guid surveyId,[FromQuery]Guid? organizationId,CancellationToken ct)=>await Enqueue(organizationId,o=>jobs.EnqueueDiagnosisClosedProcessingAsync(new(o,surveyId,UserId:UserId),HttpContext.TraceIdentifier,ct));
    [HttpPost("reprocess/response/{responseId:guid}")] public async Task<IActionResult> Response(Guid responseId,[FromQuery]Guid? organizationId,CancellationToken ct)=>await Enqueue(organizationId,o=>jobs.EnqueueResponseProcessingAsync(new(o,ResponseId:responseId,UserId:UserId),HttpContext.TraceIdentifier,ct));
    [HttpPost("recalculate")] public async Task<IActionResult> Recalculate([FromQuery]Guid? organizationId,CancellationToken ct)=>await Enqueue(organizationId,o=>jobs.EnqueueManualRecalculationAsync(new(o,UserId:UserId),HttpContext.TraceIdentifier,ct));
    private async Task<IActionResult> Enqueue(Guid? requested,Func<Guid,Task<Guid>> action){var a=await Organization(requested);if(a.Error is not null)return a.Error;var id=await action(a.Id);return Accepted(new{id,status="pending",correlationId=HttpContext.TraceIdentifier});}
    private async Task<IActionResult> WithAccess<T>(Guid? requested,Func<Guid,Task<T>> action){var a=await Organization(requested);return a.Error??Ok(await action(a.Id));}
    private async Task<(Guid Id,IActionResult? Error)> Organization(Guid? requested){var claim=Guid.TryParse(User.FindFirstValue("organization_id"),out var id)?id:(Guid?)null;var admin=User.IsInRole("admin_valora")||User.IsInRole("consultor_valora");var organization=admin?requested??claim:claim;if(organization is null||(!admin&&requested.HasValue&&requested!=claim))return(Guid.Empty,StatusCode(403,Error("ORGANIZATION_SCOPE_DENIED","Organização fora do escopo autorizado.")));if(!admin&&!await permissions.HasPermissionAsync(UserId,"organizational_intelligence.generate",organization.Value))return(Guid.Empty,StatusCode(403,Error("PERMISSION_DENIED","Seu perfil ou plano não permite gerenciar o processamento.")));return(organization.Value,null);}
    private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    private object Error(string code,string message)=>new{code,message,correlationId=HttpContext.TraceIdentifier};
}
