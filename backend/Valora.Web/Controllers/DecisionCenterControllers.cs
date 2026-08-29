using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.DecisionCenter;
using Valora.Application.Indicators;

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
public sealed class IndicatorsController(DecisionCenterService service, IndicatorService indicators,
    IndicatorTargetService targets, IndicatorMeasurementService measurements, IndicatorAlertService alerts,
    ExecutiveScorecardService scorecards, AnalyticsSnapshotService snapshots) : DecisionCenterControllerBase(service)
{
    private IActionResult MissingOrganization()=>View("~/Views/Indicators/MissingOrganization.cshtml");
    private async Task<IndicatorDashboardDto> Dashboard(CancellationToken ct)
    {
        var list=await indicators.List(OrganizationId,ct); var trendMap=new Dictionary<Guid,TrendResult>();
        foreach(var item in list) trendMap[item.Id]=await measurements.Trend(OrganizationId,item.Id,ct);
        return new(list,await targets.List(OrganizationId,null,ct),await alerts.List(OrganizationId,ct),trendMap);
    }
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():View(await Dashboard(ct));
    [HttpGet("Catalog")] public async Task<IActionResult> Catalog(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():View(await Dashboard(ct));
    [HttpGet("Targets")] public async Task<IActionResult> Targets(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():View(await Dashboard(ct));
    [HttpGet("Measurements")] public async Task<IActionResult> Measurements(Guid? indicatorId,CancellationToken ct){ViewData["IndicatorId"]=indicatorId;return OrganizationId==Guid.Empty?MissingOrganization():View(await Dashboard(ct));}
    [HttpGet("Analytics")] public async Task<IActionResult> Analytics(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():View(await Dashboard(ct));
    [HttpGet("Scorecards")] public async Task<IActionResult> Scorecards(CancellationToken ct){ViewData["Scorecards"]=OrganizationId==Guid.Empty?Array.Empty<ExecutiveScorecardDto>():await scorecards.List(OrganizationId,ct);return OrganizationId==Guid.Empty?MissingOrganization():View(await Dashboard(ct));}
    [HttpGet("Alerts")] public async Task<IActionResult> Alerts(CancellationToken ct)=>OrganizationId==Guid.Empty?MissingOrganization():View(await Dashboard(ct));
    [ValidateAntiForgeryToken,HttpPost("Create")] public async Task<IActionResult> Create(CreateIndicatorRequest request,CancellationToken ct){request.ResponsibleUserId=UserId;if(!ModelState.IsValid)return View("Catalog",await Dashboard(ct));try{await indicators.Create(OrganizationId,request,ct);TempData["Success"]="Indicador criado com fonte e rastreabilidade.";}catch(ArgumentException e){ModelState.AddModelError("",e.Message);return View("Catalog",await Dashboard(ct));}return RedirectToAction(nameof(Catalog));}
    [ValidateAntiForgeryToken,HttpPost("{id:guid}/Archive")] public async Task<IActionResult> Archive(Guid id,CancellationToken ct){await indicators.Archive(OrganizationId,id,ct);return RedirectToAction(nameof(Catalog));}
    [ValidateAntiForgeryToken,HttpPost("{id:guid}/Targets")] public async Task<IActionResult> CreateTarget(Guid id,CreateTargetRequest request,CancellationToken ct){request.ResponsibleUserId=UserId;if(!ModelState.IsValid)return View("Targets",await Dashboard(ct));try{await targets.Create(OrganizationId,id,request,ct);}catch(Exception e) when(e is ArgumentException or InvalidOperationException){ModelState.AddModelError("",e.Message);return View("Targets",await Dashboard(ct));}return RedirectToAction(nameof(Targets));}
    [ValidateAntiForgeryToken,HttpPost("{id:guid}/Measurements")] public async Task<IActionResult> CreateMeasurement(Guid id,CreateMeasurementRequest request,CancellationToken ct){request.ResponsibleUserId=UserId;if(!ModelState.IsValid)return View("Measurements",await Dashboard(ct));try{await measurements.Create(OrganizationId,id,request,ct);}catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);return View("Measurements",await Dashboard(ct));}return RedirectToAction(nameof(Measurements),new{indicatorId=id});}
    [ValidateAntiForgeryToken,HttpPost("Alerts/{id:guid}/Resolve")] public async Task<IActionResult> ResolveAlert(Guid id,CancellationToken ct){await alerts.Resolve(OrganizationId,id,UserId,ct);return RedirectToAction(nameof(Alerts));}
    [ValidateAntiForgeryToken,HttpPost("Snapshots/Create")] public async Task<IActionResult> CreateSnapshot(string name,CancellationToken ct){if(string.IsNullOrWhiteSpace(name)){ModelState.AddModelError("name","Informe um nome.");return View("Analytics",await Dashboard(ct));}await snapshots.Create(OrganizationId,UserId,name,ct);return RedirectToAction(nameof(Analytics));}
}
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
