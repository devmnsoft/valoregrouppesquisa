namespace Valora.Application.DecisionCenter;

public sealed record IntelligentAlertDto(Guid Id, string SourceType, string AlertType, string Severity, string Title, string Message, string EvidenceSummary, string Status, Guid? AssignedToUserId, DateTime? AcknowledgedAt, DateTime? ResolvedAt, DateTime CreatedAt);
public sealed record OrganizationalDecisionDto(Guid Id, Guid? DiagnosticId, Guid? ResultId, string Title, string Summary, string DecisionType, string Priority, string Status, string ImpactLevel, string EvidenceSummary, string ExpectedOutcome, Guid? ResponsibleUserId, Guid? DecidedByUserId, DateTime? DecidedAt, DateTime? DueAt, DateTime? CompletedAt, string MetadataJson, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record MetricSnapshotDto(Guid Id, string MetricCode, string MetricName, string MetricGroup, decimal? Score, decimal? PreviousScore, decimal? Delta, string Level, string Trend, string EvidenceSummary, string Interpretation, string Recommendation, string ConfidenceLimit, DateTime CalculatedAt);
public sealed record GovernanceCycleDto(Guid Id, string Name, string PeriodLabel, string Status, string? LearningSummary, DateTime OpenedAt, DateTime? ClosedAt);
public sealed record GovernanceMeetingDto(Guid Id, Guid? CycleId, string Title, string Agenda, string Status, DateTime ScheduledAt, string? MinutesSummary, string? NextSteps);
public sealed record DecisionCenterOverviewDto(int CriticalAlerts, int PendingDecisions, int RegressingMetrics, int OverdueActions, IReadOnlyList<IntelligentAlertDto> Alerts, IReadOnlyList<OrganizationalDecisionDto> Decisions, IReadOnlyList<MetricSnapshotDto> Metrics, IReadOnlyList<GovernanceCycleDto> Cycles);
public sealed record CreateDecisionRequest(string Title, string Summary, string DecisionType, string Priority, string ImpactLevel, string EvidenceSummary, string ExpectedOutcome, Guid? ResponsibleUserId, DateTime? DueAt, Guid? SourceAlertId = null, Guid? GovernanceCycleId = null);
public sealed record CreateGovernanceCycleRequest(string Name, string PeriodLabel, Guid? PrimaryDiagnosticId, DateTime OpenedAt);
public sealed record RegisterGovernanceMeetingRequest(Guid? CycleId, string Title, string Agenda, DateTime ScheduledAt, string? ParticipantsSummary, string? EvidenceSummary, string? NextSteps);

public interface IDecisionCenterRepository
{
    Task<DecisionCenterOverviewDto> GetOverviewAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<IntelligentAlertDto>> ListAlertsAsync(Guid organizationId, string? severity, string? status, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalDecisionDto>> ListDecisionsAsync(Guid organizationId, CancellationToken ct);
    Task<OrganizationalDecisionDto?> GetDecisionAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<MetricSnapshotDto>> ListMetricsAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<GovernanceCycleDto>> ListCyclesAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<GovernanceMeetingDto>> ListMeetingsAsync(Guid organizationId, CancellationToken ct);
    Task AcknowledgeAlertAsync(Guid organizationId, Guid id, Guid userId, CancellationToken ct);
    Task ResolveAlertAsync(Guid organizationId, Guid id, Guid userId, CancellationToken ct);
    Task<Guid> CreateDecisionAsync(Guid organizationId, Guid userId, CreateDecisionRequest request, CancellationToken ct);
    Task<Guid> CreateCycleAsync(Guid organizationId, Guid userId, CreateGovernanceCycleRequest request, CancellationToken ct);
    Task<Guid> RegisterMeetingAsync(Guid organizationId, Guid userId, RegisterGovernanceMeetingRequest request, CancellationToken ct);
}

public sealed class DecisionCenterService(IDecisionCenterRepository repository)
{
    public Task<DecisionCenterOverviewDto> Overview(Guid organizationId, CancellationToken ct) => repository.GetOverviewAsync(organizationId, ct);
    public Task<IReadOnlyList<IntelligentAlertDto>> Alerts(Guid organizationId, string? severity, string? status, CancellationToken ct) => repository.ListAlertsAsync(organizationId, severity, status, ct);
    public Task<IReadOnlyList<OrganizationalDecisionDto>> Decisions(Guid organizationId, CancellationToken ct) => repository.ListDecisionsAsync(organizationId, ct);
    public Task<OrganizationalDecisionDto?> Decision(Guid organizationId, Guid id, CancellationToken ct) => repository.GetDecisionAsync(organizationId, id, ct);
    public Task<IReadOnlyList<MetricSnapshotDto>> Metrics(Guid organizationId, CancellationToken ct) => repository.ListMetricsAsync(organizationId, ct);
    public Task<IReadOnlyList<GovernanceCycleDto>> Cycles(Guid organizationId, CancellationToken ct) => repository.ListCyclesAsync(organizationId, ct);
    public Task<IReadOnlyList<GovernanceMeetingDto>> Meetings(Guid organizationId, CancellationToken ct) => repository.ListMeetingsAsync(organizationId, ct);
    public Task Acknowledge(Guid organizationId, Guid id, Guid userId, CancellationToken ct) => repository.AcknowledgeAlertAsync(organizationId, id, userId, ct);
    public Task Resolve(Guid organizationId, Guid id, Guid userId, CancellationToken ct) => repository.ResolveAlertAsync(organizationId, id, userId, ct);
    public Task<Guid> CreateDecision(Guid organizationId, Guid userId, CreateDecisionRequest request, CancellationToken ct) => repository.CreateDecisionAsync(organizationId, userId, request, ct);
    public Task<Guid> CreateCycle(Guid organizationId, Guid userId, CreateGovernanceCycleRequest request, CancellationToken ct) => repository.CreateCycleAsync(organizationId, userId, request, ct);
    public Task<Guid> RegisterMeeting(Guid organizationId, Guid userId, RegisterGovernanceMeetingRequest request, CancellationToken ct) => repository.RegisterMeetingAsync(organizationId, userId, request, ct);
}
