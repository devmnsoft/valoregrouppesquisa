using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.OrganizationalArchitecture;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/architecture")]
public sealed class ArchitectureController(ArchitectureOverviewService overview, OrganizationUnitService units,
 OrganizationPositionService positions, ResponsibilityMatrixService responsibilities, BusinessProcessService processes,
 DecisionRightService decisions, OrganizationalDependencyService dependencies, ArchitectureSnapshotService snapshots,
 ArchitectureRiskService risks) : ControllerBase
{
 [HttpGet] public Task<ArchitectureSummary> Get(CancellationToken ct)=>overview.GetAsync(OrganizationId(),ct);
 [HttpGet("units")] public Task<IReadOnlyList<ArchitectureUnit>> Units(CancellationToken ct)=>units.ListAsync(OrganizationId(),ct);
 [HttpPost("units")] public async Task<IActionResult> CreateUnit(CreateUnitRequest request,CancellationToken ct)=>Created("/api/v1/architecture/units",await units.CreateAsync(OrganizationId(),request,HttpContext.TraceIdentifier,ct));
 [HttpGet("positions")] public Task<IReadOnlyList<ArchitecturePosition>> Positions(CancellationToken ct)=>positions.ListAsync(OrganizationId(),ct);
 [HttpPost("positions")] public async Task<IActionResult> CreatePosition(CreatePositionRequest request,CancellationToken ct)=>Created("/api/v1/architecture/positions",await positions.CreateAsync(OrganizationId(),request,HttpContext.TraceIdentifier,ct));
 [HttpGet("responsibilities")] public Task<IReadOnlyList<ResponsibilityItem>> Responsibilities(CancellationToken ct)=>responsibilities.ListAsync(OrganizationId(),ct);
 [HttpPost("responsibilities")] public async Task<IActionResult> CreateResponsibility(CreateResponsibilityRequest request,CancellationToken ct)=>Created("/api/v1/architecture/responsibilities",await responsibilities.CreateAsync(OrganizationId(),request,HttpContext.TraceIdentifier,ct));
 [HttpGet("processes")] public Task<IReadOnlyList<BusinessProcess>> Processes(CancellationToken ct)=>processes.ListAsync(OrganizationId(),ct);
 [HttpPost("processes")] public async Task<IActionResult> CreateProcess(CreateProcessRequest request,CancellationToken ct)=>Created("/api/v1/architecture/processes",await processes.CreateAsync(OrganizationId(),request,HttpContext.TraceIdentifier,ct));
 [HttpGet("decision-rights")] public Task<IReadOnlyList<DecisionRight>> DecisionRights(CancellationToken ct)=>decisions.ListAsync(OrganizationId(),ct);
 [HttpPost("decision-rights")] public async Task<IActionResult> CreateDecision(CreateDecisionRightRequest request,CancellationToken ct)=>Created("/api/v1/architecture/decision-rights",await decisions.CreateAsync(OrganizationId(),request,HttpContext.TraceIdentifier,ct));
 [HttpGet("dependencies")] public Task<IReadOnlyList<OrganizationalDependency>> Dependencies(CancellationToken ct)=>dependencies.ListAsync(OrganizationId(),ct);
 [HttpPost("dependencies")] public async Task<IActionResult> CreateDependency(CreateDependencyRequest request,CancellationToken ct)=>Created("/api/v1/architecture/dependencies",await dependencies.CreateAsync(OrganizationId(),request,HttpContext.TraceIdentifier,ct));
 [HttpPost("snapshot")] public async Task<IActionResult> Snapshot(CancellationToken ct)=>Created("/api/v1/architecture",await snapshots.CreateAsync(OrganizationId(),UserId(),HttpContext.TraceIdentifier,ct));
 [HttpGet("risks")] public Task<IReadOnlyList<ArchitectureRisk>> Risks(CancellationToken ct)=>risks.ListAsync(OrganizationId(),ct);
 private Guid OrganizationId(){var value=Request.Headers["X-Organization-Id"].FirstOrDefault()??User.FindFirstValue("organization_id")??User.FindFirstValue("organizationId");return Guid.TryParse(value,out var id)?id:throw new UnauthorizedAccessException("Selecione uma organização para acessar o Studio de Arquitetura.");}
 private Guid? UserId()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
}
