using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Workspace;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1")]
public sealed class WorkspaceController(IExecutiveWorkspaceService workspace,IMyDayService day,IExecutivePriorityService priorities,
    IGlobalSearchService search,IWorkspaceItemService items,IRecentItemsService recent,IQuickActionService actions) : ControllerBase
{
    private Guid OrganizationId=>Guid.TryParse(User.FindFirstValue("organization_id"),out var id)?id:Guid.Empty;
    private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    private bool OrganizationWide=>User.IsInRole("admin_valora")||User.IsInRole("admin_cliente");
    private IActionResult MissingContext()=>BadRequest(new{code="ORGANIZATION_REQUIRED",message="Selecione uma organização para abrir seu Workspace.",correlationId=HttpContext.TraceIdentifier});
    [HttpGet("workspace")] public async Task<IActionResult> Get(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingContext():Ok(await workspace.GetAsync(OrganizationId,UserId,OrganizationWide,ct));
    [HttpGet("workspace/my-day")] public async Task<IActionResult> MyDay(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingContext():Ok(await day.GetAsync(OrganizationId,UserId,OrganizationWide,ct));
    [HttpGet("workspace/priorities")] public async Task<IActionResult> Priorities(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingContext():Ok(await priorities.ListAsync(OrganizationId,UserId,OrganizationWide,ct));
    [HttpPost("workspace/priorities")] public async Task<IActionResult> CreatePriority([FromBody]CreatePriorityRequest request,CancellationToken ct){if(OrganizationId==Guid.Empty)return MissingContext();if(!ModelState.IsValid)return ValidationProblem(ModelState);return Ok(await priorities.CreateAsync(OrganizationId,UserId,request,ct));}
    [HttpGet("search/global")] public async Task<IActionResult> Search([FromQuery]string term,CancellationToken ct)=>OrganizationId==Guid.Empty?MissingContext():Ok(await search.SearchAsync(OrganizationId,UserId,term??"",OrganizationWide,ct));
    [HttpPost("workspace/pins")] public async Task<IActionResult> Pin([FromBody]PinItemRequest request,CancellationToken ct){if(OrganizationId==Guid.Empty)return MissingContext();await items.PinAsync(OrganizationId,UserId,request.ItemId,ct);return NoContent();}
    [HttpDelete("workspace/pins/{id:guid}")] public async Task<IActionResult> Unpin(Guid id,CancellationToken ct){if(OrganizationId==Guid.Empty)return MissingContext();await items.UnpinAsync(OrganizationId,UserId,id,ct);return NoContent();}
    [HttpGet("workspace/recent")] public async Task<IActionResult> Recent(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingContext():Ok(await recent.GetAsync(OrganizationId,UserId,ct));
    [HttpGet("workspace/quick-actions")] public async Task<IActionResult> Actions(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingContext():Ok(await actions.ListAsync(OrganizationId,ct));
    [HttpPost("workspace/quick-actions/{code}/execute")] public async Task<IActionResult> Execute(string code,CancellationToken ct){if(OrganizationId==Guid.Empty)return MissingContext();var action=await actions.ExecuteAsync(OrganizationId,UserId,code,ct);return action is null?NotFound(new{message="Atalho não disponível para este contexto."}):Ok(action);}
}
