using System.Text.Json;

namespace Valora.Application.OrganizationalIntelligence;

public sealed record IntelligenceModuleRecordDto(Guid Id, string? Code, string Status, JsonElement Data,
    int MethodologyVersion, int Version, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record EvidenceItemDto(Guid Id, Guid? SurveyId, Guid? ResponseId, Guid? FormId, Guid? QuestionId,
    string ConceptCode, string CapabilityCode, string DimensionCode, string? MetricCode, string? IndexCode,
    string EvidenceType, string SourceType, decimal? NormalizedValue, decimal Weight, decimal ConfidenceWeight,
    int Polarity, string? RawValueMasked, string? TextExcerpt, string MappingStatus, string MetadataJson, DateTime CreatedAt);

public sealed record DimensionHeatmapDto(Guid DimensionId, string Code, string Name, decimal Score, int EvidenceCount);
public sealed record EvidenceSummaryDto(int Responses, int ScoredResults, int Surveys, int ActionPlans, IReadOnlyList<DimensionHeatmapDto> Dimensions)
{
    public int Total => Responses + ScoredResults;
}
public sealed record OrganizationalInsightDto(Guid Id, Guid RunId, string Dimension, string Observation, string Evidence,
    string Correlation, string ProbableCause, string Impact, string Priority, string EvolutionPlan, DateTime CreatedAt);
public sealed record OrganizationalIntelligenceRunDto(Guid Id, Guid OrganizationId, decimal MaturityIndex,
    decimal CultureTrustIndex, decimal GovernanceExecutionIndex, decimal StructuralGap, string StrongestDimension,
    string WeakestDimension, int EvidenceCount, string ConfidenceLevel, string? Warning,
    IReadOnlyList<DimensionHeatmapDto> Heatmap, IReadOnlyList<OrganizationalInsightDto> Insights, DateTime CreatedAt);
public sealed record OrganizationalJourneyEventDto(Guid Id, Guid OrganizationId, string Title, string Description,
    string EventType, DateTime OccurredAt, Guid? CreatedBy, DateTime CreatedAt);
public sealed record ValoraIndicatorDefinitionDto(Guid Id, string Code, string Name, string Description,
    string Category, decimal Weight, bool IsActive);
public sealed record OrganizationalIntelligenceDashboardDto(OrganizationalIntelligenceRunDto? LatestRun,
    EvidenceSummaryDto Evidence, IReadOnlyList<OrganizationalJourneyEventDto> Journey,
    IReadOnlyList<ValoraIndicatorDefinitionDto> Indicators);
public sealed record GenerateOrganizationalIntelligenceRequest(string? Notes = null);
public sealed record CreateJourneyEventRequest(string Title, string Description, string EventType, DateTime? OccurredAt = null);
public sealed record EvolutionPointDto(DateTime CycleAt, decimal MaturityIndex, decimal Change, string Classification, bool HasSufficientHistory, decimal? EstimatedNextCycle);
public sealed record ValoraActionDto(Guid Id, Guid OrganizationId, string Code, string Title, string Description,
    string EvidenceJustification, string Capability, string Priority, string? Owner, string? ExecutiveSponsor,
    DateTime? DueAt, string Complexity, string Indicators, string ExpectedResult, string CompletionCriteria,
    string Status, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record CreateValoraActionRequest(string Title, string Description, string EvidenceJustification,
    string Capability, string Priority, string? Owner, string? ExecutiveSponsor, DateTime? DueAt,
    string Complexity, string Indicators, string ExpectedResult, string CompletionCriteria);
public sealed record UpdateValoraActionRequest(string Status, string? Owner = null, string? ExecutiveSponsor = null,
    DateTime? DueAt = null, string? Priority = null, string? Notes = null);
public sealed record ValoraActionHistoryDto(Guid Id, Guid ActionId, string Status, string Notes, Guid? ChangedBy, DateTime ChangedAt);

public interface IOrganizationalIntelligenceRepository
{
    Task<EvidenceSummaryDto> GetEvidenceAsync(Guid organizationId, CancellationToken ct);
    Task<OrganizationalIntelligenceDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> ListRunsAsync(Guid organizationId, CancellationToken ct);
    Task<OrganizationalIntelligenceRunDto?> GetRunAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task SaveAnalysisAsync(OrganizationalIntelligenceRunDto run, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalJourneyEventDto>> ListJourneyAsync(Guid organizationId, CancellationToken ct);
    Task<OrganizationalJourneyEventDto> CreateJourneyEventAsync(OrganizationalJourneyEventDto item, CancellationToken ct);
    Task<IReadOnlyList<ValoraIndicatorDefinitionDto>> ListIndicatorsAsync(CancellationToken ct);
    Task<IReadOnlyList<ValoraActionDto>> ListActionsAsync(Guid organizationId, CancellationToken ct);
    Task<ValoraActionDto> CreateActionAsync(ValoraActionDto item, Guid userId, CancellationToken ct);
    Task<ValoraActionDto?> UpdateActionAsync(Guid organizationId, Guid actionId, UpdateValoraActionRequest request, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<ValoraActionHistoryDto>> ListActionHistoryAsync(Guid organizationId, Guid actionId, CancellationToken ct);
    Task<bool> DeleteActionAsync(Guid organizationId, Guid actionId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<EvidenceItemDto>> ListEvidenceItemsAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<IntelligenceModuleRecordDto>> ListModuleRecordsAsync(Guid organizationId, string module, CancellationToken ct);
}

public interface IOrganizationalIntelligenceService
{
    Task<OrganizationalIntelligenceDashboardDto> DashboardAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> RunsAsync(Guid organizationId, CancellationToken ct);
    Task<OrganizationalIntelligenceRunDto?> RunAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<OrganizationalIntelligenceRunDto> GenerateAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalJourneyEventDto>> JourneyAsync(Guid organizationId, CancellationToken ct);
    Task<OrganizationalJourneyEventDto> CreateJourneyAsync(Guid organizationId, Guid userId, CreateJourneyEventRequest request, CancellationToken ct);
    Task<IReadOnlyList<ValoraIndicatorDefinitionDto>> IndicatorsAsync(CancellationToken ct);
    Task<IReadOnlyList<EvolutionPointDto>> EvolutionAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<ValoraActionDto>> ActionsAsync(Guid organizationId, CancellationToken ct);
    Task<ValoraActionDto> CreateActionAsync(Guid organizationId, Guid userId, CreateValoraActionRequest request, CancellationToken ct);
    Task<ValoraActionDto?> UpdateActionAsync(Guid organizationId, Guid actionId, Guid userId, UpdateValoraActionRequest request, CancellationToken ct);
    Task<IReadOnlyList<ValoraActionHistoryDto>> ActionHistoryAsync(Guid organizationId, Guid actionId, CancellationToken ct);
    Task<bool> DeleteActionAsync(Guid organizationId, Guid actionId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<EvidenceItemDto>> EvidenceItemsAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<IntelligenceModuleRecordDto>> ModuleRecordsAsync(Guid organizationId, string module, CancellationToken ct);
}
