using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Access;
using Valora.Application.RiskCompliance;

namespace Valora.Api.Controllers;

[Authorize,ApiController,Route("api/v1/risk-compliance")]
public sealed class RiskComplianceController(RiskRegisterService risks,RiskAssessmentService assessments,
    RiskControlService controls,ComplianceFrameworkService frameworks,ComplianceAssessmentService compliance,
    NonConformityService nonConformities,MitigationPlanService mitigations,RiskHeatmapService heatmap,
    ILogger<RiskComplianceController> logger):ControllerBase
{
    private Guid ActorId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    private Guid OrganizationId=>Guid.TryParse(User.FindFirstValue("organization_id"),out var claim)&&claim!=Guid.Empty?claim:
        Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault(),out var header)?header:Guid.Empty;

    [HttpGet,Authorize(Policy=ValoraPermissions.RiskCompliance.View)] public async Task<ActionResult> Dashboard(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():Ok(await risks.Dashboard(OrganizationId,ct));
    [HttpGet("risks"),Authorize(Policy=ValoraPermissions.RiskCompliance.View)] public async Task<ActionResult> Risks(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():Ok(await risks.List(OrganizationId,ct));
    [HttpPost("risks"),Authorize(Policy=ValoraPermissions.RiskCompliance.RisksManage)] public async Task<ActionResult> CreateRisk(CreateRiskRequest request,CancellationToken ct){if(!Scope(out var o))return MissingOrganization();var id=await risks.Create(o,ActorId,request,ct);return CreatedAtAction(nameof(Risks),new{id},new{id,eventName="risk.created"});}
    [HttpPut("risks/{id:guid}"),Authorize(Policy=ValoraPermissions.RiskCompliance.RisksManage)] public async Task<ActionResult> UpdateRisk(Guid id,CreateRiskRequest request,CancellationToken ct){if(!Scope(out var o))return MissingOrganization();await risks.Update(o,id,request,ct);return NoContent();}
    [HttpPost("risks/{id:guid}/assess"),Authorize(Policy=ValoraPermissions.RiskCompliance.RisksManage)] public async Task<ActionResult> AssessRisk(Guid id,AssessRiskRequest request,CancellationToken ct){if(!Scope(out var o))return MissingOrganization();await assessments.Assess(o,id,ActorId,request,ct);return Ok(new{id,eventName="risk.assessed"});}
    [HttpGet("controls"),Authorize(Policy=ValoraPermissions.RiskCompliance.View)] public async Task<ActionResult> Controls(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():Ok(await controls.List(OrganizationId,ct));
    [HttpPost("controls"),Authorize(Policy=ValoraPermissions.RiskCompliance.ControlsManage)] public async Task<ActionResult> CreateControl(CreateControlRequest request,CancellationToken ct){if(!Scope(out var o))return MissingOrganization();var id=await controls.Create(o,ActorId,request,ct);return Ok(new{id,eventName="control.created"});}
    [HttpPost("controls/{id:guid}/test"),Authorize(Policy=ValoraPermissions.RiskCompliance.ControlsManage)] public async Task<ActionResult> TestControl(Guid id,TestControlRequest request,CancellationToken ct){if(!Scope(out var o))return MissingOrganization();await controls.Test(o,id,ActorId,request,ct);return Ok(new{id,eventName="control.tested"});}
    [HttpGet("frameworks"),Authorize(Policy=ValoraPermissions.RiskCompliance.View)] public async Task<ActionResult> Frameworks(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():Ok(await frameworks.List(OrganizationId,ct));
    [HttpPost("assessments"),Authorize(Policy=ValoraPermissions.RiskCompliance.ComplianceManage)] public async Task<ActionResult> AssessCompliance(ComplianceAssessmentRequest request,CancellationToken ct){if(!Scope(out var o))return MissingOrganization();await compliance.Assess(o,ActorId,request,ct);return Ok(new{request.RequirementId,eventName="compliance.assessed"});}
    [HttpGet("non-conformities"),Authorize(Policy=ValoraPermissions.RiskCompliance.View)] public async Task<ActionResult> NonConformities(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():Ok(await nonConformities.List(OrganizationId,ct));
    [HttpPost("mitigation-plans"),Authorize(Policy=ValoraPermissions.RiskCompliance.MitigationPlansManage)] public async Task<ActionResult> CreateMitigation(CreateMitigationPlanRequest request,CancellationToken ct){if(!Scope(out var o))return MissingOrganization();var id=await mitigations.Create(o,ActorId,request,ct);return Ok(new{id,eventName="mitigation_plan.created"});}
    [HttpGet("heatmap"),Authorize(Policy=ValoraPermissions.RiskCompliance.HeatmapView)] public async Task<ActionResult> Heatmap(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():Ok(await heatmap.Get(OrganizationId,ct));
    private bool Scope(out Guid id){id=OrganizationId;if(id==Guid.Empty)logger.LogWarning("Risk Compliance request rejected without organization. CorrelationId {CorrelationId}",HttpContext.TraceIdentifier);return id!=Guid.Empty;}
    private ObjectResult MissingOrganization()=>Problem(statusCode:400,title:"Organização não selecionada",detail:"Selecione uma organização antes de acessar riscos e conformidade.",extensions:new Dictionary<string,object?>{{"correlationId",HttpContext.TraceIdentifier}});
}
