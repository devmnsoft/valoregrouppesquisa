using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Advisor;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/advisor")]
public sealed class AdvisorController(AdvisorConversationService conversations,AdvisorMessageService messages,
    AdvisorContextBuilderService context,AdvisorPromptTemplateService templates,AdvisorFeedbackService feedback,
    AdvisorActionSuggestionService suggestions) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    private Guid OrganizationId
    {
        get
        {
            if(Guid.TryParse(User.FindFirstValue("organization_id"),out var claimId)&&claimId!=Guid.Empty)return claimId;
            return Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault(),out var headerId)?headerId:Guid.Empty;
        }
    }
    [HttpGet("conversations")] public async Task<ActionResult> List(CancellationToken ct)=>Ok(await conversations.List(OrganizationId,UserId,ct));
    [HttpPost("conversations")] public async Task<ActionResult> Create([FromBody]CreateAdvisorConversationRequest request,CancellationToken ct){var id=await conversations.Create(OrganizationId,UserId,request,ct);return CreatedAtAction(nameof(Get),new{id},new{id,eventName="advisor.conversation.created"});}
    [HttpGet("conversations/{id:guid}")] public async Task<ActionResult> Get(Guid id,CancellationToken ct){var result=await conversations.Get(OrganizationId,UserId,id,ct);return result is null?NotFound():Ok(result);}
    [HttpPost("conversations/{id:guid}/messages")] public async Task<ActionResult> Send(Guid id,[FromBody]SendAdvisorMessageRequest request,CancellationToken ct)=>Ok(await messages.Send(OrganizationId,UserId,id,request,ct));
    [HttpGet("context-options")] public async Task<ActionResult> ContextOptions(CancellationToken ct)=>Ok(await context.Options(OrganizationId,ct));
    [HttpGet("templates")] public async Task<ActionResult> Templates(CancellationToken ct)=>Ok(await templates.List(OrganizationId,ct));
    [HttpPost("templates"),Authorize(Roles="admin,admin_valora,super_admin")] public async Task<ActionResult> CreateTemplate([FromBody]CreateAdvisorTemplateRequest request,CancellationToken ct)=>Ok(new{id=await templates.Create(OrganizationId,UserId,request,ct)});
    [HttpPost("messages/{id:guid}/feedback")] public async Task<ActionResult> Feedback(Guid id,[FromBody]AdvisorFeedbackRequest request,CancellationToken ct){await feedback.Create(OrganizationId,UserId,id,request,ct);return Ok(new{id,eventName="advisor.feedback.created"});}
    [HttpPost("suggestions/{id:guid}/create-action")] public ActionResult CreateAction(Guid id,[FromQuery]bool confirmed=false){suggestions.EnsureConfirmation(confirmed);return Accepted(new{id,eventName="advisor.suggestion.converted_to_action",message="Conversão confirmada e encaminhada ao ActionCenter."});}
    [HttpPost("suggestions/{id:guid}/create-decision")] public ActionResult CreateDecision(Guid id,[FromQuery]bool confirmed=false){suggestions.EnsureConfirmation(confirmed);return Accepted(new{id,eventName="advisor.suggestion.converted_to_decision",message="Conversão confirmada e encaminhada ao DecisionCenter."});}
}
