using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.DecisionCenter;

namespace Valora.Web.Controllers;

[Authorize]
public abstract class DecisionCenterControllerBase(DecisionCenterService service) : Controller
{
    protected DecisionCenterService Service { get; } = service;
    protected Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id") ?? User.FindFirstValue("organizationId"), out var id) ? id : Guid.Empty;
    protected Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}

[Route("DecisionCenter")]
public sealed class DecisionCenterController(DecisionCenterService service) : DecisionCenterControllerBase(service)
{
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct) => View(await Service.Overview(OrganizationId, ct));
    [HttpGet("Alerts")] public async Task<IActionResult> Alerts(string? severity,string? status,CancellationToken ct) => View(await Service.Alerts(OrganizationId,severity,status,ct));
    [ValidateAntiForgeryToken,HttpPost("Alerts/{id:guid}/Acknowledge")] public async Task<IActionResult> Acknowledge(Guid id,CancellationToken ct){await Service.Acknowledge(OrganizationId,id,UserId,ct);return RedirectToAction(nameof(Alerts));}
    [ValidateAntiForgeryToken,HttpPost("Alerts/{id:guid}/Resolve")] public async Task<IActionResult> Resolve(Guid id,CancellationToken ct){await Service.Resolve(OrganizationId,id,UserId,ct);return RedirectToAction(nameof(Alerts));}
}
[Route("Decisions")]
public sealed class DecisionsController(DecisionCenterService service) : DecisionCenterControllerBase(service)
{
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>View(await Service.Decisions(OrganizationId,ct));
    [HttpGet("Details/{id:guid}")] public async Task<IActionResult> Details(Guid id,CancellationToken ct){var item=await Service.Decision(OrganizationId,id,ct);return item is null?NotFound():View(item);}
    [ValidateAntiForgeryToken,HttpPost("Create")] public async Task<IActionResult> Create(CreateDecisionRequest request,CancellationToken ct){if(string.IsNullOrWhiteSpace(request.EvidenceSummary)){ModelState.AddModelError(nameof(request.EvidenceSummary),"Uma decisão precisa estar vinculada a evidências verificáveis.");return View("Index",await Service.Decisions(OrganizationId,ct));}var id=await Service.CreateDecision(OrganizationId,UserId,request,ct);return RedirectToAction(nameof(Details),new{id});}
}
[Route("Indicators")]
public sealed class IndicatorsController(DecisionCenterService service) : DecisionCenterControllerBase(service)
{ [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>View(await Service.Metrics(OrganizationId,ct)); }
[Route("Governance")]
public sealed class GovernanceController(DecisionCenterService service) : DecisionCenterControllerBase(service)
{
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>View(new GovernanceViewModel(await Service.Cycles(OrganizationId,ct),await Service.Meetings(OrganizationId,ct)));
    [HttpGet("Cycles")] public async Task<IActionResult> Cycles(CancellationToken ct)=>View(await Service.Cycles(OrganizationId,ct));
    [HttpGet("Cycles/Details/{id:guid}")] public async Task<IActionResult> CycleDetails(Guid id,CancellationToken ct){var item=(await Service.Cycles(OrganizationId,ct)).FirstOrDefault(x=>x.Id==id);return item is null?NotFound():View(item);}
    [ValidateAntiForgeryToken,HttpPost("Cycles/Create")] public async Task<IActionResult> CreateCycle(CreateGovernanceCycleRequest request,CancellationToken ct){await Service.CreateCycle(OrganizationId,UserId,request,ct);return RedirectToAction(nameof(Cycles));}
    [HttpGet("Meetings")] public async Task<IActionResult> Meetings(CancellationToken ct)=>View(await Service.Meetings(OrganizationId,ct));
    [HttpGet("Meetings/Create")] public IActionResult CreateMeeting()=>View();
    [ValidateAntiForgeryToken,HttpPost("Meetings/Create")] public async Task<IActionResult> CreateMeeting(RegisterGovernanceMeetingRequest request,CancellationToken ct){var id=await Service.RegisterMeeting(OrganizationId,UserId,request,ct);return RedirectToAction(nameof(MeetingDetails),new{id});}
    [HttpGet("Meetings/Details/{id:guid}")] public async Task<IActionResult> MeetingDetails(Guid id,CancellationToken ct){var item=(await Service.Meetings(OrganizationId,ct)).FirstOrDefault(x=>x.Id==id);return item is null?NotFound():View(item);}
}
public sealed record GovernanceViewModel(IReadOnlyList<GovernanceCycleDto> Cycles,IReadOnlyList<GovernanceMeetingDto> Meetings);
