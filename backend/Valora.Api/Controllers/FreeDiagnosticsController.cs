using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.DTOs.FreeDiagnostics;
using Valora.Application.FreeDiagnostics;

namespace Valora.Api.Controllers;

[ApiController]
[Authorize]
public sealed class FreeDiagnosticsController(FreeDiagnosticsAppService service, ILogger<FreeDiagnosticsController> logger) : ControllerBase
{
    string? CorrelationId => HttpContext.Response.Headers.TryGetValue("X-Correlation-Id", out var id) ? id.ToString() : HttpContext.TraceIdentifier;
    [HttpGet("/admin/free-diagnostics")]
    public async Task<IActionResult> List([FromQuery] DateTime? startDate,[FromQuery] DateTime? endDate,[FromQuery] string? name,[FromQuery] string? email,[FromQuery] string? emailStatus,[FromQuery] string? maturityLevel,[FromQuery] string? certificateStatus,[FromQuery] int page=1,[FromQuery] int pageSize=50)
    { try { return Ok(new { ok=true, items=await service.ListAsync(new(startDate,endDate,name,email,emailStatus,maturityLevel,certificateStatus,page,pageSize), User), correlationId=CorrelationId }); } catch(Exception ex){ logger.LogError(ex,"Falha ao listar diagnósticos gratuitos. CorrelationId={CorrelationId}",CorrelationId); return StatusCode(500,new{ok=false,message="Erro operacional ao listar diagnósticos gratuitos.",correlationId=CorrelationId}); } }
    [HttpGet("/admin/free-diagnostics/summary")]
    public async Task<IActionResult> Summary([FromQuery] DateTime? startDate,[FromQuery] DateTime? endDate,[FromQuery] string? name,[FromQuery] string? email,[FromQuery] string? emailStatus,[FromQuery] string? maturityLevel,[FromQuery] string? certificateStatus)
    { try { return Ok(new { ok=true, summary=await service.SummaryAsync(new(startDate,endDate,name,email,emailStatus,maturityLevel,certificateStatus), User), correlationId=CorrelationId }); } catch(Exception ex){ logger.LogError(ex,"Falha ao resumir diagnósticos gratuitos. CorrelationId={CorrelationId}",CorrelationId); return StatusCode(500,new{ok=false,message="Erro operacional ao resumir diagnósticos gratuitos.",correlationId=CorrelationId}); } }
    [HttpGet("/admin/free-diagnostics/{responseId:guid}")]
    public async Task<IActionResult> Detail(Guid responseId) { try { return Ok(new { ok=true, item=await service.DetailAsync(responseId,User), correlationId=CorrelationId }); } catch(Exception ex){ logger.LogError(ex,"Falha ao buscar diagnóstico gratuito. ResponseId={ResponseId} CorrelationId={CorrelationId}",responseId,CorrelationId); return StatusCode(500,new{ok=false,message="Erro operacional ao buscar diagnóstico gratuito.",correlationId=CorrelationId}); } }
    [HttpPost("/admin/free-diagnostics/{responseId:guid}/resend-email")]
    public async Task<IActionResult> Resend(Guid responseId,[FromBody] ResendResultEmailRequest request) => Accepted(await service.ResendAsync(responseId,request,User,CorrelationId));
    [HttpPost("/admin/free-diagnostics/{responseId:guid}/regenerate-certificate")]
    public async Task<IActionResult> Regenerate(Guid responseId,[FromBody] RegenerateCertificateRequest request) => Accepted(await service.RegenerateCertificateAsync(responseId,request,User,CorrelationId));
    [HttpPost("/admin/free-diagnostics/{responseId:guid}/mark-communication-reviewed")]
    public async Task<IActionResult> MarkReviewed(Guid responseId,[FromBody] MarkCommunicationReviewedRequest request) => Accepted(await service.MarkReviewedAsync(responseId,request,User,CorrelationId));
}
