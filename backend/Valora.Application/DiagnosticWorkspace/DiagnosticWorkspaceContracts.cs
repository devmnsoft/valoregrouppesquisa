using System.Text.Json;

namespace Valora.Application.DiagnosticWorkspace;

public sealed record DiagnosticCycleDto(Guid Id, Guid OrganizationId, Guid SurveyId, Guid? FormId, string Title,
    string? Description, int CycleNumber, string MethodologyVersion, string Status, DateTime? OpenedAt,
    DateTime? PublishedAt, DateTime? ClosedAt, DateTime? ProcessedAt, DateTime? ReportGeneratedAt,
    int ResponseCount, int EvidenceCount, decimal? ConfidenceLevel, string ProcessingStatus, string? PublicUrl,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record DiagnosticWorkspaceItemDto(Guid Id, string? Code, string Status, JsonElement Data,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record DiagnosticWorkspaceEvidenceDto(Guid Id, Guid? ResponseId, Guid? QuestionId, string ConceptCode,
    string? MetricCode, string? IndexCode, string EvidenceType, decimal? NormalizedValue, decimal Weight,
    decimal ConfidenceWeight, string MappingStatus, string? MaskedAnswer, DateTime CreatedAt);
public sealed record DiagnosticWorkspaceOverviewDto(DiagnosticCycleDto Cycle, DiagnosticWorkspaceItemDto? LastJob,
    IReadOnlyList<DiagnosticWorkspaceItemDto> MainIndices, IReadOnlyList<DiagnosticWorkspaceItemDto> MainInsights,
    IReadOnlyList<DiagnosticWorkspaceItemDto> RecommendedActions, IReadOnlyList<DiagnosticWorkspaceItemDto> Alerts);
public sealed record DiagnosticWorkspaceDto(DiagnosticWorkspaceOverviewDto Overview,
    IReadOnlyList<DiagnosticWorkspaceEvidenceDto> Evidence, IReadOnlyList<DiagnosticWorkspaceItemDto> Metrics,
    IReadOnlyList<DiagnosticWorkspaceItemDto> Indices, IReadOnlyList<DiagnosticWorkspaceItemDto> Inferences,
    IReadOnlyList<DiagnosticWorkspaceItemDto> Insights, IReadOnlyList<DiagnosticWorkspaceItemDto> Actions);
public sealed record DiagnosticWorkspaceCommandDto(Guid CycleId, string Status, string Message, Guid? JobId = null, bool HasLimitation = false);

public interface IDiagnosticWorkspaceRepository
{
    Task<DiagnosticCycleDto?> GetCycleAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticWorkspaceEvidenceDto>> EvidenceAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticWorkspaceItemDto>> ModuleAsync(Guid organizationId, Guid cycleId, Guid surveyId, string module, CancellationToken ct);
    Task<DiagnosticWorkspaceItemDto?> LastJobAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<bool> HasActiveJobAsync(Guid organizationId, Guid surveyId, CancellationToken ct);
    Task<Guid> MarkProcessingAsync(Guid organizationId, DiagnosticCycleDto cycle, Guid userId, string correlationId, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto?> CloseAsync(Guid organizationId, DiagnosticCycleDto cycle, Guid userId, string correlationId, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto> GenerateReportAsync(Guid organizationId, DiagnosticCycleDto cycle, Guid userId, bool preview, CancellationToken ct);
}

public interface IDiagnosticWorkspaceService
{
    Task<DiagnosticWorkspaceDto?> GetWorkspaceAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<DiagnosticWorkspaceOverviewDto?> GetOverviewAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticWorkspaceEvidenceDto>?> GetEvidenceAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticWorkspaceItemDto>?> GetModuleAsync(Guid organizationId, Guid id, string module, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto?> ProcessAsync(Guid organizationId, Guid id, Guid userId, string correlationId, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto?> CloseCycleAsync(Guid organizationId, Guid id, Guid userId, string correlationId, CancellationToken ct);
    Task<DiagnosticWorkspaceCommandDto?> GenerateReportAsync(Guid organizationId, Guid id, Guid userId, bool preview, CancellationToken ct);
}
