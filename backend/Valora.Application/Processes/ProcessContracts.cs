using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Processes;

public sealed record ProcessDefinitionDto(Guid Id, string Name, string Description, string Category, Guid OwnerUserId,
    string Status, int VersionNumber, int StepCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record ProcessStepDto(Guid Id, Guid ProcessDefinitionId, string Name, string? Description, string StepType,
    int OrderIndex, Guid? ResponsibleRoleId, int? SlaHours, bool RequiresApproval, bool EvidenceRequired);
public sealed record ProcessInstanceDto(Guid Id, Guid ProcessDefinitionId, string ProcessName, string Title, string Status,
    string Priority, Guid? CurrentStepId, string? CurrentStepName, Guid ResponsibleUserId, string SourceType,
    DateTimeOffset StartedAt, DateTimeOffset? DueAt);
public sealed record ProcessDashboardDto(int ActiveProcesses, int RunningInstances, int SlaAtRisk, int SlaOverdue,
    int PendingApprovals, int Bottlenecks, IReadOnlyList<ProcessInstanceDto> RecentInstances);
public sealed record ProcessSlaDto(Guid InstanceId, string ProcessName, string InstanceTitle, string StepName,
    string ResponsibleName, DateTimeOffset DueAt, string Risk);
public sealed record ProcessInsightDto(Guid Id, Guid ProcessDefinitionId, string ProcessName, Guid? StepId,
    string? StepName, string InsightType, string EvidenceSummary, decimal? AverageDurationHours, int OccurrenceCount);
public sealed record ProcessTemplateDto(Guid Id, string Name, string Description, string Category, string Status);

public sealed class CreateProcessRequest
{
    [Required, StringLength(160)] public string Name { get; init; } = "";
    [StringLength(2000)] public string Description { get; init; } = "";
    [Required, StringLength(80)] public string Category { get; init; } = "";
    [Required] public Guid OwnerUserId { get; init; }
}
public sealed class CreateProcessStepRequest
{
    [Required, StringLength(160)] public string Name { get; init; } = "";
    [StringLength(2000)] public string? Description { get; init; }
    [Required, RegularExpression("^(initial|task|approval|automation|final)$")] public string StepType { get; init; } = "task";
    [Range(0, 10000)] public int OrderIndex { get; init; }
    public Guid? ResponsibleRoleId { get; init; }
    [Range(1, 8760)] public int? SlaHours { get; init; }
    public bool RequiresApproval { get; init; }
    public bool EvidenceRequired { get; init; }
}
public sealed class CreateProcessInstanceRequest
{
    [Required] public Guid ProcessDefinitionId { get; init; }
    [Required, StringLength(180)] public string Title { get; init; } = "";
    [Required] public Guid ResponsibleUserId { get; init; }
    [Required, RegularExpression("^(manual|action|decision|insight|diagnostic|strategy)$")] public string SourceType { get; init; } = "manual";
    public Guid? SourceId { get; init; }
    [Required, RegularExpression("^(low|medium|high|critical)$")] public string Priority { get; init; } = "medium";
}
public sealed class AdvanceProcessRequest { public bool EvidenceProvided { get; init; } }
public sealed class ApprovalDecisionRequest
{
    [StringLength(2000)] public string? Justification { get; init; }
    public Guid? ReturnStepId { get; init; }
}

public interface IProcessDefinitionRepository
{
    Task<IReadOnlyList<ProcessDefinitionDto>> List(Guid organizationId, CancellationToken ct);
    Task<ProcessDefinitionDto?> Get(Guid organizationId, Guid id, CancellationToken ct);
    Task<Guid> Create(Guid organizationId, Guid userId, CreateProcessRequest request, CancellationToken ct);
    Task Publish(Guid organizationId, Guid id, Guid userId, CancellationToken ct);
    Task<Guid> NewVersion(Guid organizationId, Guid id, Guid userId, CancellationToken ct);
}
public interface IProcessStepRepository { Task<IReadOnlyList<ProcessStepDto>> List(Guid organizationId, Guid processId, CancellationToken ct); Task<Guid> Create(Guid organizationId, Guid processId, CreateProcessStepRequest request, CancellationToken ct); }
public interface IProcessInstanceRepository { Task<IReadOnlyList<ProcessInstanceDto>> List(Guid organizationId, CancellationToken ct); Task<ProcessInstanceDto?> Get(Guid organizationId, Guid id, CancellationToken ct); Task<Guid> Create(Guid organizationId, Guid userId, CreateProcessInstanceRequest request, CancellationToken ct); Task ChangeState(Guid organizationId, Guid id, Guid userId, string operation, bool evidenceProvided, CancellationToken ct); Task<ProcessDashboardDto> Dashboard(Guid organizationId, CancellationToken ct); }
public interface IProcessApprovalRepository { Task Decide(Guid organizationId, Guid approvalId, Guid userId, string decision, ApprovalDecisionRequest request, CancellationToken ct); }
public interface IProcessSlaRepository { Task<IReadOnlyList<ProcessSlaDto>> List(Guid organizationId, CancellationToken ct); }
public interface IProcessAutomationRepository { Task RecordExecution(Guid organizationId, Guid ruleId, string status, string? technicalMessage, CancellationToken ct); }
public interface IProcessInsightRepository { Task<IReadOnlyList<ProcessInsightDto>> List(Guid organizationId, CancellationToken ct); }
public interface IProcessTemplateRepository { Task<IReadOnlyList<ProcessTemplateDto>> List(Guid organizationId, CancellationToken ct); }
