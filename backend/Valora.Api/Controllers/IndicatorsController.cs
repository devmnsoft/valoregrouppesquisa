using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Indicators;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/indicators")]
public sealed class IndicatorsController(IndicatorService indicators, IndicatorTargetService targets,
    IndicatorMeasurementService measurements, IndicatorAlertService alerts) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id") ?? Request.Headers["X-Organization-Id"].ToString(), out var id) ? id : Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    [HttpGet] public async Task<ActionResult> List(CancellationToken ct) => Ok(await indicators.List(OrganizationId,ct));
    [HttpPost] public async Task<ActionResult> Create([FromBody]CreateIndicatorRequest r,CancellationToken ct){var id=await indicators.Create(OrganizationId,r,ct);return CreatedAtAction(nameof(Get),new{id},new{id,eventName="indicator.created"});}
    [HttpGet("{id:guid}")] public async Task<ActionResult> Get(Guid id,CancellationToken ct){var value=await indicators.Get(OrganizationId,id,ct);return value is null?NotFound():Ok(value);}
    [HttpPost("{id:guid}/targets")] public async Task<ActionResult> CreateTarget(Guid id,[FromBody]CreateTargetRequest r,CancellationToken ct)=>Ok(new{id=await targets.Create(OrganizationId,id,r,ct),eventName="indicator.target.created"});
    [HttpPost("{id:guid}/measurements")] public async Task<ActionResult> CreateMeasurement(Guid id,[FromBody]CreateMeasurementRequest r,CancellationToken ct)=>Ok(new{id=await measurements.Create(OrganizationId,id,r,ct),eventName="indicator.measurement.created"});
    [HttpGet("{id:guid}/trend")] public async Task<ActionResult> Trend(Guid id,CancellationToken ct)=>Ok(await measurements.Trend(OrganizationId,id,ct));
    [HttpGet("alerts")] public async Task<ActionResult> Alerts(CancellationToken ct)=>Ok(await alerts.List(OrganizationId,ct));
    [HttpPost("alerts/{id:guid}/resolve")] public async Task<ActionResult> Resolve(Guid id,CancellationToken ct){await alerts.Resolve(OrganizationId,id,UserId,ct);return Ok(new{id,eventName="indicator.alert.resolved"});}
}

[Authorize, ApiController, Route("api/v1/scorecards")]
public sealed class ScorecardsController(ExecutiveScorecardService service) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id") ?? Request.Headers["X-Organization-Id"].ToString(),out var id)?id:Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    [HttpGet] public async Task<ActionResult> List(CancellationToken ct)=>Ok(await service.List(OrganizationId,ct));
    [HttpPost] public async Task<ActionResult> Create(CreateScorecardRequest request,CancellationToken ct)=>Ok(new{id=await service.Create(OrganizationId,UserId,request,ct),eventName="scorecard.created"});
}

[Authorize, ApiController, Route("api/v1/analytics/snapshots")]
public sealed class AnalyticsSnapshotsController(AnalyticsSnapshotService service) : ControllerBase
{
    private Guid OrganizationId => Guid.TryParse(User.FindFirstValue("organization_id") ?? Request.Headers["X-Organization-Id"].ToString(),out var id)?id:Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;
    [HttpGet] public async Task<ActionResult> List(CancellationToken ct)=>Ok(await service.List(OrganizationId,ct));
    [HttpPost] public async Task<ActionResult> Create([FromBody]CreateSnapshotRequest request,CancellationToken ct)=>Ok(new{id=await service.Create(OrganizationId,UserId,request.Name,ct),eventName="analytics.snapshot.created"});
}
public sealed record CreateSnapshotRequest([property: Required, StringLength(160)] string Name);
