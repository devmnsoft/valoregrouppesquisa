using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DiagnosticWorkspace;

namespace Valora.Api.Controllers;

[Authorize,ApiController,Route("api/v1/diagnostics/{id:guid}/workspace")]
public sealed class DiagnosticWorkspaceController(IDiagnosticWorkspaceService service,IPermissionService permissions,IEntitlementService entitlements):ControllerBase
{
    [HttpGet] public Task<IActionResult> Workspace(Guid id,CancellationToken ct)=>Read(id,(o)=>service.GetWorkspaceAsync(o,id,ct));
    [HttpGet("overview")] public Task<IActionResult> Overview(Guid id,CancellationToken ct)=>Read(id,(o)=>service.GetOverviewAsync(o,id,ct));
    [HttpGet("/api/v1/diagnostics/{id:guid}/participation")] public Task<IActionResult> Participation(Guid id,CancellationToken ct)=>Read(id,o=>service.GetModuleAsync(o,id,"participation",ct));
    [HttpGet("evidence")] public Task<IActionResult> Evidence(Guid id,CancellationToken ct)=>Read(id,(o)=>service.GetEvidenceAsync(o,id,ct));
    [HttpGet("{module:regex(^responses|metrics|indices|inferences|insights|actions|heatmap|radar|evolution|journey|benchmark|report|governance$)}")]
    public Task<IActionResult> Module(Guid id,string module,CancellationToken ct)=>Read(id,o=>service.GetModuleAsync(o,id,module,ct));
    [HttpPost("process")] public Task<IActionResult> Process(Guid id,CancellationToken ct)=>Write(id,"organizational_intelligence.generate",o=>service.ProcessAsync(o,id,UserId,HttpContext.TraceIdentifier,ct));
    [HttpPost("close")] public Task<IActionResult> Close(Guid id,CancellationToken ct)=>Write(id,"surveys.manage",o=>service.CloseCycleAsync(o,id,UserId,HttpContext.TraceIdentifier,ct));
    [HttpPost("report/preview")] public Task<IActionResult> Preview(Guid id,CancellationToken ct)=>Write(id,"reports.generate",o=>service.GenerateReportAsync(o,id,UserId,true,ct));
    [HttpPost("report/generate")] public Task<IActionResult> Generate(Guid id,CancellationToken ct)=>Write(id,"reports.generate",o=>service.GenerateReportAsync(o,id,UserId,false,ct));
    [HttpPost("/api/v1/diagnostics/{id:guid}/executive-report/preview")] public Task<IActionResult> ExecutivePreview(Guid id,CancellationToken ct)=>Preview(id,ct);
    [HttpPost("/api/v1/diagnostics/{id:guid}/executive-report/generate")] public Task<IActionResult> ExecutiveGenerate(Guid id,CancellationToken ct)=>Generate(id,ct);
    [HttpGet("/api/v1/diagnostics/{id:guid}/executive-report")] public Task<IActionResult> ExecutiveReport(Guid id,CancellationToken ct)=>Read(id,o=>service.GetModuleAsync(o,id,"report",ct));
    private async Task<IActionResult> Read<T>(Guid id,Func<Guid,Task<T?>> action){var access=await Access("organizational_intelligence.read");if(access.Error is not null)return access.Error;var value=await action(access.OrganizationId);return value is null?NotFound(new{code="DIAGNOSTIC_NOT_FOUND",message="Diagnóstico não encontrado.",correlationId=HttpContext.TraceIdentifier}):Ok(value);}
    private async Task<IActionResult> Write<T>(Guid id,string permission,Func<Guid,Task<T?>> action){var access=await Access(permission);if(access.Error is not null)return access.Error;var value=await action(access.OrganizationId);return value is null?NotFound(new{code="DIAGNOSTIC_NOT_FOUND",message="Diagnóstico não encontrado.",correlationId=HttpContext.TraceIdentifier}):Ok(value);}
    private async Task<(Guid OrganizationId,IActionResult? Error)> Access(string permission){if(!Guid.TryParse(User.FindFirstValue("organization_id"),out var o))return(Guid.Empty,ForbidProblem("ORGANIZATION_REQUIRED","Selecione uma organização para abrir o diagnóstico."));if(!IsAdmin&&(!await permissions.HasPermissionAsync(UserId,permission,o)||!await entitlements.CanUseAsync(o,"organizational_intelligence")))return(o,ForbidProblem("WORKSPACE_FORBIDDEN","Este recurso faz parte dos módulos avançados do Valora Insight™ Profissional."));return(o,null);}
    private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    private bool IsAdmin=>User.IsInRole("admin_valora")||User.Claims.Any(x=>(x.Type is ClaimTypes.Role or "role")&&x.Value.Equals("admin_valora",StringComparison.OrdinalIgnoreCase));
    private ObjectResult ForbidProblem(string code,string message)=>StatusCode(403,new{code,message,correlationId=HttpContext.TraceIdentifier});
}
