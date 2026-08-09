namespace Valora.Application.OrganizationalIntelligence;

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
}
