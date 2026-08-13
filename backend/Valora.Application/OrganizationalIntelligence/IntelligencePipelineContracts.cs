namespace Valora.Application.OrganizationalIntelligence;

public sealed record IntelligenceProcessingContext(Guid OrganizationId, Guid? SurveyId = null, Guid? ResponseId = null,
    Guid? FormId = null, Guid? UserId = null, Guid? SourceEntityId = null, string Trigger = "response_received");
public sealed record ProcessingStageResult(string Stage, int Records, bool SufficientEvidence, string Message,
    IReadOnlyList<Guid> EvidenceIds);
public sealed record IntelligencePipelineResult(Guid RunId, string Trigger, IReadOnlyList<ProcessingStageResult> Stages,
    DateTime CompletedAt)
{
    public bool HasSufficientEvidence => Stages.Any(x => x.SufficientEvidence);
}

public interface IIntelligencePipelineRepository
{
    Task<IReadOnlyList<Guid>> ExtractResponseEvidenceAsync(IntelligenceProcessingContext context, CancellationToken ct);
    Task<ProcessingStageResult> CalculateMetricsAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct);
    Task<ProcessingStageResult> CalculateIndicesAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct);
    Task<ProcessingStageResult> GenerateInferencesAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct);
    Task<ProcessingStageResult> GenerateInsightsAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct);
    Task<ProcessingStageResult> RefreshProjectionAsync(IntelligenceProcessingContext context, string module, IReadOnlyList<Guid> evidenceIds, CancellationToken ct);
    Task RecordEventAsync(IntelligenceProcessingContext context, Guid runId, string eventType, string title, string description, CancellationToken ct);
}

public interface IOrganizationalIntelligencePipeline
{
    Task<IntelligencePipelineResult> ProcessResponseAsync(IntelligenceProcessingContext context, CancellationToken ct);
    Task<IntelligencePipelineResult> ProcessDiagnosisClosedAsync(IntelligenceProcessingContext context, CancellationToken ct);
    Task<IntelligencePipelineResult> ProcessActionAsync(IntelligenceProcessingContext context, bool completed, CancellationToken ct);
    Task<IntelligencePipelineResult> ProcessExecutiveReportAsync(IntelligenceProcessingContext context, CancellationToken ct);
}
public interface IEvidenceExtractionService { Task<ProcessingStageResult> ExtractAsync(IntelligenceProcessingContext context, CancellationToken ct); }
public interface IMetricCalculationService { Task<ProcessingStageResult> CalculateAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IValoraIndexCalculationService { Task<ProcessingStageResult> CalculateAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IInferenceEngine { Task<ProcessingStageResult> InferAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IInsightGenerationService { Task<ProcessingStageResult> GenerateAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IActionRecommendationService { Task<ProcessingStageResult> RecommendAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IEvolutionService { Task<ProcessingStageResult> RefreshAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IJourneyService { Task RecordAsync(IntelligenceProcessingContext context, Guid runId, string type, string title, string description, CancellationToken ct); }
public interface IHeatmapService { Task<ProcessingStageResult> RefreshAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IRadarService { Task<ProcessingStageResult> RefreshAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IBenchmarkService { Task<ProcessingStageResult> RefreshAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface IExecutiveReportService { Task<ProcessingStageResult> SnapshotAsync(IntelligenceProcessingContext context, IReadOnlyList<Guid> evidenceIds, CancellationToken ct); }
public interface INotificationService { Task NotifyAsync(IntelligenceProcessingContext context, Guid runId, string type, string title, string message, CancellationToken ct); }
public interface IPlatformGovernanceService { Task RecordAsync(IntelligenceProcessingContext context, Guid runId, string type, string description, CancellationToken ct); }
