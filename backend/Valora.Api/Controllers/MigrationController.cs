using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[ApiController]
[Authorize]
public sealed class MigrationController(
    IMigrationBatchRepository batches,
    IMigrationSourceFileRepository sources,
    IMigrationDryRunService dryRun,
    IMigrationApplyService apply,
    IMigrationReconciliationService reconciliation,
    IMigrationConflictRepository conflicts,
    IMigrationRollbackService rollback,
    ICutoverReadinessService cutover,
    IMigrationReportService reports) : ControllerBase
{
    static string Role(System.Security.Claims.ClaimsPrincipal user) => user.FindFirst("role")?.Value ?? user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "admin_valora";
    IActionResult Safe(Func<Task<object?>> action){try{return Ok(new{ok=true,data=action().GetAwaiter().GetResult()});}catch(Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException){return BadRequest(new{ok=false,error=ex.Message,correlationId=HttpContext.TraceIdentifier});}}
    [HttpGet("/admin/migration/status")] public IActionResult Status() => Ok(new { ok = true, dataProvider = "api-postgresql-local", postgresSchema = "valorapesquisa", firebase = "preserved", sensitivePayload = "masked" });
    [HttpPost("/migration/sources")] public IActionResult RegisterSource([FromBody] MigrationUploadRequest r)=>Safe(async()=> await sources.CreateAsync(null,r.FileName,r.ContentType,System.Text.Encoding.UTF8.GetByteCount(r.PayloadJson),Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(r.PayloadJson))).ToLowerInvariant(),null,"registered"));
    [HttpGet("/migration/sources")] public IActionResult GetSources()=>Safe(async()=>await sources.ListAsync());
    [HttpGet("/migration/sources/{id:guid}")] public IActionResult GetSource(Guid id)=>Safe(async()=>await sources.GetAsync(id));
    [HttpPost("/migration/batches")] public IActionResult CreateBatch([FromBody] MigrationUploadRequest r)=>Safe(async()=>await batches.CreateAsync(r.SourceType,r.SourceName,"dry_run",User.Identity?.Name??"admin_valora"));
    [HttpGet("/migration/batches")] public IActionResult GetBatches()=>Safe(async()=>await batches.ListAsync());
    [HttpGet("/migration/batches/{id:guid}")] public IActionResult GetBatch(Guid id)=>Safe(async()=>await batches.GetAsync(id));
    [HttpPost("/migration/batches/{batchId:guid}/dry-run")] public IActionResult DryRun(Guid batchId,[FromBody] MigrationDryRunRequest r)=>Safe(async()=>await dryRun.ExecuteAsync(r with{BatchId=batchId,ConfirmDryRun=true}));
    [HttpGet("/migration/batches/{batchId:guid}/dry-run/report")] public IActionResult DryRunReport(Guid batchId)=>Safe(async()=>await reports.GetDryRunReportAsync(batchId));
    [HttpPost("/migration/batches/{batchId:guid}/apply")] public IActionResult Apply(Guid batchId,[FromBody] MigrationApplyRequest r)=>Safe(async()=>await apply.ApplyAsync(r with{BatchId=batchId,RequestedByRole=Role(User)}));
    [HttpPost("/migration/batches/{batchId:guid}/reconcile")] public IActionResult Reconcile(Guid batchId)=>Safe(async()=>await reconciliation.ReconcileAsync(batchId));
    [HttpGet("/migration/batches/{batchId:guid}/reconciliation")] public IActionResult Reconciliation(Guid batchId)=>Safe(async()=>await reports.GetReconciliationAsync(batchId));
    [HttpGet("/migration/batches/{batchId:guid}/conflicts")] public IActionResult Conflicts(Guid batchId)=>Safe(async()=>await conflicts.ListByBatchAsync(batchId));
    [HttpPatch("/migration/conflicts/{conflictId:guid}/resolution")] public IActionResult Resolve(Guid conflictId,[FromBody] Dictionary<string,string> body)=>Safe(async()=>{await conflicts.ResolveAsync(conflictId,body.GetValueOrDefault("resolution")??"manual_review",User.Identity?.Name??"admin_valora"); return new{resolved=true};});
    [HttpPost("/migration/batches/{batchId:guid}/rollback")] public IActionResult Rollback(Guid batchId,[FromBody] MigrationRollbackRequest r)=>Safe(async()=>await rollback.RollbackAsync(r with{BatchId=batchId,RequestedByRole=Role(User)}));
    [HttpGet("/migration/batches/{batchId:guid}/rollback/report")] public IActionResult RollbackReport(Guid batchId)=>Safe(async()=>await rollback.GetReportAsync(batchId));
    [HttpGet("/migration/batches/{batchId:guid}/cutover-readiness")] public IActionResult Cutover(Guid batchId)=>Safe(async()=>await cutover.GetAsync(batchId));
}
