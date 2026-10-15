using System.Text.Json;

namespace Valora.Application.DiagnosticWorkspace;

public sealed record DiagnosticCycleDto(Guid Id, Guid OrganizationId, Guid SurveyId, Guid? FormId, string Title,
    string? Description, int CycleNumber, string MethodologyVersion, string Status, DateTime? OpenedAt,
    DateTime? PublishedAt, DateTime? ClosedAt, DateTime? ProcessedAt, DateTime? ReportGeneratedAt,
    int ResponseCount, int EvidenceCount, int MappedEvidenceCount, int PendingEvidenceCount,
    decimal? ConfidenceLevel, string ProcessingStatus, string? PublicUrl, DateTime CreatedAt, DateTime UpdatedAt,
    string? FormName = null, int? FormVersionNumber = null, DateTime? StartsAt = null, DateTime? ExpiresAt = null,
    string? TargetAudience = null, string? DistributionChannel = null, string? ResponsibleName = null,
    Guid? ResponsibleUserId = null, Guid? FormVersionId = null, int RowVersion = 1);
public sealed record DiagnosticWorkspaceItemDto(Guid Id, string? Code, string Status, JsonElement Data,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record DiagnosticWorkspaceEvidenceDto(Guid Id, Guid? ResponseId, Guid? QuestionId, string ConceptCode,
    string? MetricCode, string? IndexCode, string EvidenceType, decimal? NormalizedValue, decimal Weight,
    decimal ConfidenceWeight, string MappingStatus, string? MaskedAnswer, DateTime CreatedAt);
public sealed record DiagnosticLinkedPlanDto(Guid Id, string Title, string Status, string Priority, string OriginType,
    Guid? OwnerUserId, string? OwnerName, DateTime? DueAt, int ProgressPercent, int ActiveItemCount, DateTime CreatedAt);
public sealed record DiagnosticLinkedActionItemDto(Guid Id, Guid ActionPlanId, string PlanTitle, string Title,
    string Status, string Priority, Guid? ResponsibleUserId, string? ResponsibleName, DateTime? DueAt,
    int ProgressPercent, string EvidenceSummary, string ExpectedOutcome, string? CompletionEvidence,
    string? CompletionResult, bool IsOverdue, DateTime CreatedAt);
public sealed record DiagnosticExecutionHistoryDto(string Kind, Guid EntityId, string Title, string Operation,
    string? FromValue, string? ToValue, string? Reason, string? ActorName, DateTime ChangedAt);
public sealed record DiagnosticWorkspaceExecutionDto(IReadOnlyList<DiagnosticLinkedPlanDto> Plans,
    IReadOnlyList<DiagnosticLinkedActionItemDto> Items, IReadOnlyList<DiagnosticExecutionHistoryDto> History,
    bool HasMorePlans = false, bool HasMoreItems = false, bool HasMoreHistory = false);
public sealed record DiagnosticWorkspaceOverviewDto(DiagnosticCycleDto Cycle, DiagnosticWorkspaceItemDto? LastJob,
    IReadOnlyList<DiagnosticWorkspaceItemDto> MainIndices, IReadOnlyList<DiagnosticWorkspaceItemDto> MainInsights,
    IReadOnlyList<DiagnosticWorkspaceItemDto> RecommendedActions, IReadOnlyList<DiagnosticWorkspaceItemDto> Alerts);
public sealed record DiagnosticWorkspaceDto(DiagnosticWorkspaceOverviewDto Overview,
    IReadOnlyList<DiagnosticWorkspaceEvidenceDto> Evidence, IReadOnlyList<DiagnosticWorkspaceItemDto> Metrics,
    IReadOnlyList<DiagnosticWorkspaceItemDto> Indices, IReadOnlyList<DiagnosticWorkspaceItemDto> Inferences,
    IReadOnlyList<DiagnosticWorkspaceItemDto> Insights, IReadOnlyList<DiagnosticWorkspaceItemDto> Actions);
public sealed record DiagnosticWorkspaceCommandDto(Guid CycleId, string Status, string Message, Guid? JobId = null, bool HasLimitation = false);

public sealed record EligibleUserDto(Guid Id, string Name, string Email, string? Department = null);
public sealed record FormVersionOptionDto(Guid Id, int Version, string? Title, string Status, bool IsImmutable, DateTime CreatedAt);
public sealed record DiagnosticWorkspacePreparationOptionsDto(
    IReadOnlyList<EligibleUserDto> EligibleUsers,
    IReadOnlyList<FormVersionOptionDto> AvailableVersions,
    bool CanChangeMethodology,
    string? MethodologyLockReason = null);

public sealed record UpdateDiagnosticPreparationDto(
    string Title,
    string? Description,
    string? TargetAudience,
    Guid? ResponsibleUserId,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    Guid? FormVersionId,
    int RowVersion);

public class DiagnosticConcurrencyException : Exception {
    public DiagnosticConcurrencyException(string message) : base(message) { }
}

public interface IDiagnosticWorkspaceRepository {
    Task<DiagnosticCycleDto?> GetCycleAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticWorkspaceEvidenceDto>> EvidenceAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<DiagnosticWorkspaceExecutionDto> ExecutionAsync(Guid organizationId, DiagnosticCycleDto cycle, Guid userId, bool organizationWide, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticWorkspaceItemDto>> ModuleAsync(Guid organizationId, Guid cycleId, Guid surveyId, string module, CancellationToken ct);
    Task<DiagnosticWorkspaceItemDto?> LastJobAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<bool> HasActiveJobAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<Guid> MarkProcessingAsync(Guid organizationId, DiagnosticCycleDto cycle, Guid userId, string correlationId, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto?> CloseAsync(Guid organizationId, DiagnosticCycleDto cycle, Guid userId, string correlationId, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto> GenerateReportAsync(Guid organizationId, DiagnosticCycleDto cycle, Guid userId, bool preview, CancellationToken ct);
    Task<DiagnosticWorkspacePreparationOptionsDto> GetPreparationOptionsAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<DiagnosticCycleDto> UpdatePreparationAsync(Guid organizationId, Guid surveyId, UpdateDiagnosticPreparationDto dto, Guid userId, string correlationId, CancellationToken ct);
}

public interface IDiagnosticWorkspaceService {
    Task<DiagnosticWorkspaceDto?> GetWorkspaceAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<DiagnosticWorkspaceOverviewDto?> GetOverviewAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticWorkspaceEvidenceDto>?> GetEvidenceAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<DiagnosticWorkspaceExecutionDto?> GetExecutionAsync(Guid organizationId, Guid id, Guid userId, bool organizationWide, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticWorkspaceItemDto>?> GetModuleAsync(Guid organizationId, Guid id, string module, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto?> ProcessAsync(Guid organizationId, Guid id, Guid userId, string correlationId, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto?> CloseCycleAsync(Guid organizationId, Guid id, Guid userId, string correlationId, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto?> GenerateReportAsync(Guid organizationId, Guid id, Guid userId, bool preview, CancellationToken ct);
    Task<DiagnosticWorkspacePreparationOptionsDto?> GetPreparationOptionsAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<DiagnosticCycleDto?> UpdatePreparationAsync(Guid organizationId, Guid surveyId, UpdateDiagnosticPreparationDto dto, Guid userId, string correlationId, CancellationToken ct);
}
