using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.ActionCenter;
namespace Valora.Web.Controllers;

[Authorize]
[Route("ActionCenter")]
public sealed class ActionCenterController(ActionPlanService plans, ActionItemService items, ActionProgressService progress) : Controller {
    Guid O => Guid.TryParse(User.FindFirstValue("organization_id") ?? User.FindFirstValue("organizationId"), out var x) ? x : Guid.Empty; Guid U => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var x) ? x : Guid.Empty;
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken c) => View(await plans.Dashboard(O, c)); [HttpGet("Plans")] public async Task<IActionResult> Plans(CancellationToken c) => View(await plans.List(O, c)); [HttpGet("Plans/Create")] public IActionResult CreatePlan(string? originType, Guid? originId) => View(new CreateActionPlanRequest("", "", originType == "ai_insight" ? originType : "manual", originType == "ai_insight" ? originId : null, null, null, null, "medium", null, null, null, "", "")); [ValidateAntiForgeryToken, HttpPost("Plans/Create")] public async Task<IActionResult> CreatePlan(CreateActionPlanRequest r, CancellationToken c) { if (!ModelState.IsValid) return View(r); try { var id = await plans.Create(O, U, r, c); return RedirectToAction(nameof(PlanDetails), new { id }); } catch (ArgumentException e) { ModelState.AddModelError("", e.Message); return View(r); } }
    [HttpGet("Plans/Details/{id:guid}")] public async Task<IActionResult> PlanDetails(Guid id, CancellationToken c) { var p = await plans.Get(O, id, c); return p is null ? NotFound() : View(new ActionPlanDetailsViewModel(p, await items.List(O, id, c))); }
    [ValidateAntiForgeryToken, HttpPost("Items/Create")] public async Task<IActionResult> CreateItem(CreateActionItemRequest r, CancellationToken c) { var id = await items.Create(O, U, r, c); return RedirectToAction(nameof(ItemDetails), new { id }); }
    [HttpGet("Items")] public async Task<IActionResult> Items(CancellationToken c) => View(await items.List(O, null, c)); [HttpGet("Items/Details/{id:guid}")] public async Task<IActionResult> ItemDetails(Guid id, int historyPage, CancellationToken c) { var i = await items.Details(O, id, historyPage, c); return i is null ? NotFound() : View(i); }
    [ValidateAntiForgeryToken, HttpPost("Items/{id:guid}/Progress")] public async Task<IActionResult> Update(Guid id, ProgressActionRequest request, CancellationToken c) => await ExecuteCommand(id, () => progress.Update(O,U,id,request,c));
    [ValidateAntiForgeryToken, HttpPost("Items/{id:guid}/Block")] public async Task<IActionResult> Block(Guid id, TransitionActionRequest request, CancellationToken c) => await ExecuteCommand(id, () => progress.Block(O,U,id,request,c));
    [ValidateAntiForgeryToken, HttpPost("Items/{id:guid}/Complete")] public async Task<IActionResult> Complete(Guid id, CompleteActionRequest request, CancellationToken c) => await ExecuteCommand(id, () => progress.Complete(O,U,id,request,c));
    private async Task<IActionResult> ExecuteCommand(Guid id, Func<Task> command) { if (!ModelState.IsValid) { TempData["ActionError"]="Revise os campos informados."; return RedirectToAction(nameof(ItemDetails),new{id}); } try { await command(); TempData["ActionSuccess"]="Registro atualizado com sucesso."; } catch (Valora.Application.Exceptions.ConcurrencyConflictException e) { TempData["ActionError"]=e.Message; } catch (Valora.Application.Exceptions.ConflictAppException e) { TempData["ActionError"]=e.Message; } catch (KeyNotFoundException) { return NotFound(); } catch (ArgumentException e) { TempData["ActionError"]=e.Message; } return RedirectToAction(nameof(ItemDetails),new{id}); }
}
[Authorize] public sealed class ActionPlansController : Controller { [HttpGet("ActionPlans")][HttpGet("ValoraAction")] public IActionResult Index() => Redirect("/ActionCenter/Plans"); }
public sealed record ActionPlanDetailsViewModel(ActionPlanDto Plan, IReadOnlyList<ActionItemDto> Items);
