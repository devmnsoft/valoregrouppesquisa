using System.ComponentModel.DataAnnotations;

namespace Valora.Application.OrganizationalArchitecture;

public sealed record ArchitectureSummary(int Units, int ActiveProcesses, int CriticalProcesses, int UnownedResponsibilities, int ConcentratedDecisions, int CriticalDependencies, int OpenRisks, decimal MaturityScore);
public sealed record ArchitectureUnit(Guid Id, Guid OrganizationId, Guid? ParentUnitId, string Name, string UnitType, string Status, Guid? OwnerUserId);
public sealed class CreateUnitRequest { public Guid? ParentUnitId { get; init; } [Required, StringLength(160)] public string Name { get; init; } = ""; [Required] public string UnitType { get; init; } = "department"; [Required] public string Status { get; init; } = "active"; public Guid? OwnerUserId { get; init; } }
public sealed record ArchitecturePosition(Guid Id, Guid OrganizationId, Guid UnitId, string Title, string Status);
public sealed class CreatePositionRequest { [Required] public Guid UnitId { get; init; } [Required, StringLength(160)] public string Title { get; init; } = ""; }
public sealed record ResponsibilityItem(Guid Id, Guid ProcessId, Guid PersonProfileId, Guid UnitId, string ResponsibilityType, string Status);
public sealed class CreateResponsibilityRequest { [Required] public Guid MatrixId { get; init; } [Required] public Guid ProcessId { get; init; } [Required] public Guid PersonProfileId { get; init; } [Required] public Guid UnitId { get; init; } [Required, RegularExpression("Responsible|Accountable|Consulted|Informed")] public string ResponsibilityType { get; init; } = "Responsible"; }
public sealed record BusinessProcess(Guid Id, Guid OrganizationId, Guid UnitId, string Name, Guid? OwnerPersonId, string Status, string Criticality, string? IndicatorReference, string CompletenessStatus);
public sealed class CreateProcessRequest { [Required] public Guid UnitId { get; init; } [Required, StringLength(180)] public string Name { get; init; } = ""; [Required] public Guid OwnerPersonId { get; init; } [Required] public string Criticality { get; init; } = "medium"; public string? IndicatorReference { get; init; } }
public sealed record DecisionRight(Guid Id, string Scope, string DecisionType, Guid? ResponsiblePersonId, Guid? ApproverPersonId, string DecisionLimit, string Status);
public sealed class CreateDecisionRightRequest { [Required, StringLength(300)] public string Scope { get; init; } = ""; [Required] public string DecisionType { get; init; } = "operational"; [Required] public Guid ResponsiblePersonId { get; init; } [Required] public Guid ApproverPersonId { get; init; } [Required, StringLength(300)] public string DecisionLimit { get; init; } = ""; }
public sealed record OrganizationalDependency(Guid Id, Guid? SourceUnitId, Guid? TargetUnitId, Guid? SourceProcessId, Guid? TargetProcessId, string DependencyType, string Impact, string Criticality, string? MitigationPlan, string Status);
public sealed class CreateDependencyRequest { public Guid? SourceUnitId { get; init; } public Guid? TargetUnitId { get; init; } public Guid? SourceProcessId { get; init; } public Guid? TargetProcessId { get; init; } [Required] public string DependencyType { get; init; } = "operational"; [Required] public string Impact { get; init; } = ""; [Required] public string Criticality { get; init; } = "medium"; public string? MitigationPlan { get; init; } }
public sealed record ArchitectureRisk(Guid Id, string RiskType, string Title, string ProbableCause, string Impact, string Evidence, string RecommendedAction, string Severity, string Status, DateTimeOffset DetectedAt);
public sealed record ArchitectureSnapshot(Guid Id, decimal MaturityScore, DateTimeOffset CreatedAt, string CorrelationId);

public interface IOrganizationalArchitectureRepository
{
 Task<ArchitectureSummary> SummaryAsync(Guid organizationId, CancellationToken ct); Task<IReadOnlyList<ArchitectureUnit>> UnitsAsync(Guid organizationId, CancellationToken ct); Task<ArchitectureUnit> CreateUnitAsync(Guid organizationId, CreateUnitRequest request, string correlationId, CancellationToken ct);
 Task<IReadOnlyList<ArchitecturePosition>> PositionsAsync(Guid organizationId, CancellationToken ct); Task<ArchitecturePosition> CreatePositionAsync(Guid organizationId, CreatePositionRequest request, string correlationId, CancellationToken ct);
 Task<IReadOnlyList<ResponsibilityItem>> ResponsibilitiesAsync(Guid organizationId, CancellationToken ct); Task<ResponsibilityItem> CreateResponsibilityAsync(Guid organizationId, CreateResponsibilityRequest request, string correlationId, CancellationToken ct);
 Task<IReadOnlyList<BusinessProcess>> ProcessesAsync(Guid organizationId, CancellationToken ct); Task<BusinessProcess> CreateProcessAsync(Guid organizationId, CreateProcessRequest request, string correlationId, CancellationToken ct);
 Task<IReadOnlyList<DecisionRight>> DecisionRightsAsync(Guid organizationId, CancellationToken ct); Task<DecisionRight> CreateDecisionRightAsync(Guid organizationId, CreateDecisionRightRequest request, string correlationId, CancellationToken ct);
 Task<IReadOnlyList<OrganizationalDependency>> DependenciesAsync(Guid organizationId, CancellationToken ct); Task<OrganizationalDependency> CreateDependencyAsync(Guid organizationId, CreateDependencyRequest request, string correlationId, CancellationToken ct);
 Task<IReadOnlyList<ArchitectureRisk>> RisksAsync(Guid organizationId, CancellationToken ct); Task<ArchitectureSnapshot> SnapshotAsync(Guid organizationId, Guid? actorId, string correlationId, CancellationToken ct);
}
