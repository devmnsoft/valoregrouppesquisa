using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Workspace;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1")]
public sealed class WorkspaceController(IExecutiveWorkspaceService workspace, IMyDayService day, IExecutivePriorityService priorities,
    IGlobalSearchService search, IWorkspaceItemService items, IRecentItemsService recent, IQuickActionService actions,
    ICurrentRequestContext requestContext) : ControllerBase {
    private CurrentRequestContext Context => requestContext.GetCurrent();
    private Guid OrganizationId => Context.EffectiveOrganizationId ?? Guid.Empty;
    private Guid UserId => Context.UserId ?? Guid.Empty;
    private bool OrganizationWide => Context.IsGlobalAdministrator || Context.Roles.Contains("admin_cliente", StringComparer.OrdinalIgnoreCase);
    private IActionResult MissingContext() => BadRequest(new { code = "ORGANIZATION_REQUIRED", message = "Selecione uma organização para abrir seu Workspace.", correlationId = HttpContext.TraceIdentifier });
    private IActionResult? InvalidContext() => OrganizationId == Guid.Empty ? MissingContext() : UserId == Guid.Empty ? Unauthorized(new { code = "USER_REQUIRED", message = "Sua sessão precisa ser renovada.", correlationId = HttpContext.TraceIdentifier }) : null;
    [HttpGet("workspace")] public async Task<IActionResult> Get(CancellationToken ct) => InvalidContext() is { } error ? error : Ok(await workspace.GetAsync(OrganizationId, UserId, OrganizationWide, ct));
    [HttpGet("workspace/my-day")] public async Task<IActionResult> MyDay(CancellationToken ct) => InvalidContext() is { } error ? error : Ok(await day.GetAsync(OrganizationId, UserId, OrganizationWide, ct));
    [HttpGet("workspace/priorities")] public async Task<IActionResult> Priorities(CancellationToken ct) => InvalidContext() is { } error ? error : Ok(await priorities.ListAsync(OrganizationId, UserId, OrganizationWide, ct));
    [HttpPost("workspace/priorities")] public async Task<IActionResult> CreatePriority([FromBody] CreatePriorityRequest request, CancellationToken ct) { if (InvalidContext() is { } error) return error; if (!ModelState.IsValid) return ValidationProblem(ModelState); return Ok(await priorities.CreateAsync(OrganizationId, UserId, request, ct)); }
    [HttpGet("search/global")] public async Task<IActionResult> Search([FromQuery] string term, CancellationToken ct) => InvalidContext() is { } error ? error : Ok(await search.SearchAsync(OrganizationId, UserId, term ?? "", OrganizationWide, ct));
    [HttpPost("workspace/pins")] public async Task<IActionResult> Pin([FromBody] PinItemRequest request, CancellationToken ct) { if (OrganizationId == Guid.Empty) return MissingContext(); if (UserId == Guid.Empty) return Unauthorized(); if (request.ItemId == Guid.Empty) return ValidationProblem("Informe um item válido."); await items.PinAsync(OrganizationId, UserId, request.ItemId, OrganizationWide, ct); return NoContent(); }
    [HttpDelete("workspace/pins/{id:guid}")] public async Task<IActionResult> Unpin(Guid id, CancellationToken ct) { if (InvalidContext() is { } error) return error; if (id == Guid.Empty) return ValidationProblem("Informe um item válido."); await items.UnpinAsync(OrganizationId, UserId, id, ct); return NoContent(); }
    [HttpGet("workspace/recent")] public async Task<IActionResult> Recent(CancellationToken ct) => OrganizationId == Guid.Empty ? MissingContext() : UserId == Guid.Empty ? Unauthorized() : Ok(await recent.GetAsync(OrganizationId, UserId, OrganizationWide, ct));
    [HttpGet("workspace/quick-actions")] public async Task<IActionResult> Actions(CancellationToken ct) => InvalidContext() is { } error ? error : Ok(await actions.ListAsync(OrganizationId, ct));
    [HttpPost("workspace/quick-actions/{code}/execute")] public async Task<IActionResult> Execute(string code, CancellationToken ct) { if (InvalidContext() is { } error) return error; var action = await actions.ExecuteAsync(OrganizationId, UserId, code, ct); return action is null ? NotFound(new { message = "Atalho não disponível para este contexto." }) : Ok(action); }
}
