using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Benchmarks;

namespace Valora.Api.Controllers;

[Authorize,ApiController,Route("api/v1/benchmarks")]
public sealed class BenchmarksController(BenchmarkCohortService cohorts,BenchmarkSnapshotService snapshots,BenchmarkComparisonService comparisons,BenchmarkInsightService insights,BenchmarkPrivacyService privacy,BenchmarkExportService exports):ControllerBase
{
    private Guid UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)&&id!=Guid.Empty?id:Guid.Empty;
    private Guid OrganizationId=>Guid.TryParse(User.FindFirstValue("organization_id"),out var claim)&&claim!=Guid.Empty?claim:Guid.TryParse(Request.Headers["X-Organization-Id"].FirstOrDefault(),out var header)&&header!=Guid.Empty?header:Guid.Empty;
    [HttpGet] public async Task<ActionResult> Dashboard(CancellationToken ct){var history=await snapshots.List(OrganizationId,ct);var warnings=history.Count==0?["Ainda não há snapshots reais para comparação."]:Array.Empty<string>();return Ok(new BenchmarkDashboardDto(history.FirstOrDefault(),history,await insights.List(OrganizationId,ct),warnings));}
    [HttpGet("cohorts")] public async Task<ActionResult> Cohorts(CancellationToken ct)=>Ok(await cohorts.List(OrganizationId,ct));
    [HttpPost("cohorts")] public async Task<ActionResult> CreateCohort(CreateBenchmarkCohortRequest request,CancellationToken ct){var id=await cohorts.Create(OrganizationId,UserId,request,ct);return Created($"/api/v1/benchmarks/cohorts/{id}",new{id});}
    [HttpGet("snapshots")] public async Task<ActionResult> Snapshots(CancellationToken ct)=>Ok(await snapshots.List(OrganizationId,ct));
    [HttpPost("snapshots/generate")] public async Task<ActionResult> Generate(GenerateBenchmarkSnapshotRequest request,CancellationToken ct)=>Ok(new{id=await snapshots.Generate(OrganizationId,UserId,request,ct),eventName="benchmark.snapshot.generated"});
    [HttpPost("compare")] public async Task<ActionResult> Compare(BenchmarkComparisonRequest request,CancellationToken ct)=>Ok(await comparisons.Compare(OrganizationId,UserId,request,ct));
    [HttpGet("insights")] public async Task<ActionResult> Insights(CancellationToken ct)=>Ok(await insights.List(OrganizationId,ct));
    [HttpPost("insights/{id:guid}/convert-to-action")] public async Task<ActionResult> Convert(Guid id,CancellationToken ct)=>Ok(new{actionId=await insights.ConvertToAction(OrganizationId,id,UserId,ct),eventName="benchmark.insight.converted_to_action"});
    [HttpGet("privacy-rules")] public async Task<ActionResult> Privacy(CancellationToken ct)=>Ok(await privacy.Get(OrganizationId,ct));
    [HttpPost("export")] public async Task<ActionResult> Export(BenchmarkExportRequest request,CancellationToken ct)=>Accepted(new{id=await exports.Export(OrganizationId,UserId,request,ct),eventName="benchmark.export.generated"});
}
