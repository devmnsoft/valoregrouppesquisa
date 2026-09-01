using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Processes;

namespace Valora.Api.Controllers;

[Authorize,ApiController,Route("api/v1/processes")]
public sealed class ProcessesController(ProcessDefinitionService definitions,ProcessStepService steps,
    ProcessInstanceService instances,ProcessApprovalService approvals,ProcessSlaService sla,
    ProcessBottleneckInsightService insights,ProcessTemplateService templates):ControllerBase
{
    private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    private Guid OrganizationId=>Guid.TryParse(User.FindFirstValue("organization_id"),out var claim)&&claim!=Guid.Empty?claim:
        Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault(),out var header)?header:Guid.Empty;
    [HttpGet] public async Task<ActionResult> List(CancellationToken ct)=>Ok(await definitions.List(OrganizationId,ct));
    [HttpPost] public async Task<ActionResult> Create(CreateProcessRequest request,CancellationToken ct){var id=await definitions.Create(OrganizationId,UserId,request,ct);return CreatedAtAction(nameof(Get),new{id},new{id,eventName="process.created",message="Processo salvo com sucesso."});}
    [HttpGet("{id:guid}")] public async Task<ActionResult> Get(Guid id,CancellationToken ct){var item=await definitions.Get(OrganizationId,id,ct);return item is null?NotFound():Ok(item);}
    [HttpPut("{id:guid}")] public ActionResult Update(Guid id)=>Conflict(new{message="Crie uma nova versão para alterar um processo publicado.",id});
    [HttpPost("{id:guid}/publish")] public async Task<ActionResult> Publish(Guid id,CancellationToken ct){await definitions.Publish(OrganizationId,id,UserId,ct);return Ok(new{id,eventName="process.published",message="Processo publicado. Alterações estruturais exigem nova versão."});}
    [HttpPost("{id:guid}/new-version")] public async Task<ActionResult> NewVersion(Guid id,CancellationToken ct)=>Ok(new{id=await definitions.NewVersion(OrganizationId,id,UserId,ct),eventName="process.version.created"});
    [HttpGet("{id:guid}/steps")] public async Task<ActionResult> Steps(Guid id,CancellationToken ct)=>Ok(await steps.List(OrganizationId,id,ct));
    [HttpPost("{id:guid}/steps")] public async Task<ActionResult> AddStep(Guid id,CreateProcessStepRequest request,CancellationToken ct)=>Ok(new{id=await steps.Create(OrganizationId,id,request,ct)});
    [HttpGet("dashboard")] public async Task<ActionResult> Dashboard(CancellationToken ct)=>Ok(await instances.Dashboard(OrganizationId,ct));
    [HttpGet("instances")] public async Task<ActionResult> Instances(CancellationToken ct)=>Ok(await instances.List(OrganizationId,ct));
    [HttpPost("instances")] public async Task<ActionResult> Start(CreateProcessInstanceRequest request,CancellationToken ct)=>Ok(new{id=await instances.Create(OrganizationId,UserId,request,ct),eventName="process.instance.started"});
    [HttpGet("instances/{id:guid}")] public async Task<ActionResult> Instance(Guid id,CancellationToken ct){var item=await instances.Get(OrganizationId,id,ct);return item is null?NotFound():Ok(item);}
    [HttpPost("instances/{id:guid}/advance")] public async Task<ActionResult> Advance(Guid id,AdvanceProcessRequest request,CancellationToken ct){await instances.ChangeState(OrganizationId,id,UserId,"advance",request.EvidenceProvided,ct);return Ok(new{id,eventName="process.step.advanced"});}
    [HttpPost("instances/{id:guid}/pause")] public Task<ActionResult> Pause(Guid id,CancellationToken ct)=>State(id,"pause",ct);
    [HttpPost("instances/{id:guid}/complete")] public Task<ActionResult> Complete(Guid id,CancellationToken ct)=>State(id,"complete",ct);
    [HttpPost("instances/{id:guid}/cancel")] public Task<ActionResult> Cancel(Guid id,CancellationToken ct)=>State(id,"cancel",ct);
    [HttpPost("approvals/{id:guid}/approve")] public Task<ActionResult> Approve(Guid id,ApprovalDecisionRequest request,CancellationToken ct)=>Decision(id,"approved",request,ct);
    [HttpPost("approvals/{id:guid}/reject")] public Task<ActionResult> Reject(Guid id,ApprovalDecisionRequest request,CancellationToken ct)=>Decision(id,"rejected",request,ct);
    [HttpPost("approvals/{id:guid}/return")] public Task<ActionResult> Return(Guid id,ApprovalDecisionRequest request,CancellationToken ct)=>Decision(id,"returned",request,ct);
    [HttpGet("sla")] public async Task<ActionResult> Sla(CancellationToken ct)=>Ok(await sla.List(OrganizationId,ct));
    [HttpGet("insights")] public async Task<ActionResult> Insights(CancellationToken ct)=>Ok(await insights.List(OrganizationId,ct));
    [HttpGet("templates")] public async Task<ActionResult> Templates(CancellationToken ct)=>Ok(await templates.List(OrganizationId,ct));
    private async Task<ActionResult> State(Guid id,string state,CancellationToken ct){await instances.ChangeState(OrganizationId,id,UserId,state,false,ct);return Ok(new{id,eventName=$"process.instance.{state}"});}
    private async Task<ActionResult> Decision(Guid id,string decision,ApprovalDecisionRequest request,CancellationToken ct){await approvals.Decide(OrganizationId,id,UserId,decision,request,ct);return Ok(new{id,message=decision=="approved"?"A aprovação foi registrada e a execução pode continuar.":"A decisão foi registrada com rastreabilidade."});}
}
