namespace Valora.Application.OrganizationalIntelligence;

public sealed class EvidenceExtractionService(IIntelligencePipelineRepository repository) : IEvidenceExtractionService
{
    public async Task<ProcessingStageResult> ExtractAsync(IntelligenceProcessingContext context, CancellationToken ct)
    {
        var ids = await repository.ExtractResponseEvidenceAsync(context, ct);
        return new("evidence", ids.Count, ids.Count >= 3, ids.Count == 0
            ? "A resposta não possui perguntas com mapeamento metodológico; nenhum cálculo foi realizado."
            : ids.Count < 3 ? "Dados insuficientes para uma inferência moderada." : "Evidências metodológicas extraídas e rastreáveis.", ids);
    }
}
public sealed class MetricCalculationService(IIntelligencePipelineRepository repository) : IMetricCalculationService
{ public Task<ProcessingStageResult> CalculateAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => repository.CalculateMetricsAsync(c, e, ct); }
public sealed class ValoraIndexCalculationService(IIntelligencePipelineRepository repository) : IValoraIndexCalculationService
{ public Task<ProcessingStageResult> CalculateAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => repository.CalculateIndicesAsync(c, e, ct); }
public sealed class EvidenceInferenceEngine(IIntelligencePipelineRepository repository) : IInferenceEngine
{ public Task<ProcessingStageResult> InferAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => repository.GenerateInferencesAsync(c, e, ct); }
public sealed class InsightGenerationService(IIntelligencePipelineRepository repository) : IInsightGenerationService
{ public Task<ProcessingStageResult> GenerateAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => repository.GenerateInsightsAsync(c, e, ct); }

public abstract class ProjectionService(IIntelligencePipelineRepository repository, string module)
{
    protected Task<ProcessingStageResult> Refresh(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => repository.RefreshProjectionAsync(c, module, e, ct);
}
public sealed class ActionRecommendationService(IIntelligencePipelineRepository r) : ProjectionService(r, "action"), IActionRecommendationService
{ public Task<ProcessingStageResult> RecommendAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => Refresh(c, e, ct); }
public sealed class EvolutionService(IIntelligencePipelineRepository r) : ProjectionService(r, "evolution"), IEvolutionService
{ public Task<ProcessingStageResult> RefreshAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => Refresh(c, e, ct); }
public sealed class HeatmapService(IIntelligencePipelineRepository r) : ProjectionService(r, "heatmap"), IHeatmapService
{ public Task<ProcessingStageResult> RefreshAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => Refresh(c, e, ct); }
public sealed class RadarService(IIntelligencePipelineRepository r) : ProjectionService(r, "radar"), IRadarService
{ public Task<ProcessingStageResult> RefreshAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => Refresh(c, e, ct); }
public sealed class BenchmarkService(IIntelligencePipelineRepository r) : ProjectionService(r, "benchmark"), IBenchmarkService
{ public Task<ProcessingStageResult> RefreshAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => Refresh(c, e, ct); }
public sealed class ExecutiveReportService(IIntelligencePipelineRepository r) : ProjectionService(r, "executive_report"), IExecutiveReportService
{ public Task<ProcessingStageResult> SnapshotAsync(IntelligenceProcessingContext c, IReadOnlyList<Guid> e, CancellationToken ct) => Refresh(c, e, ct); }
public sealed class JourneyService(IIntelligencePipelineRepository repository) : IJourneyService
{ public Task RecordAsync(IntelligenceProcessingContext c, Guid run, string type, string title, string description, CancellationToken ct) => repository.RecordEventAsync(c, run, type, title, description, ct); }
public sealed class NotificationService(IIntelligencePipelineRepository repository) : INotificationService
{ public Task NotifyAsync(IntelligenceProcessingContext c, Guid run, string type, string title, string message, CancellationToken ct) => repository.RecordEventAsync(c, run, $"notification:{type}", title, message, ct); }
public sealed class PlatformGovernanceService(IIntelligencePipelineRepository repository) : IPlatformGovernanceService
{ public Task RecordAsync(IntelligenceProcessingContext c, Guid run, string type, string description, CancellationToken ct) => repository.RecordEventAsync(c, run, $"governance:{type}", "Evento rastreável do pipeline", description, ct); }

public sealed class OrganizationalIntelligencePipeline(IEvidenceExtractionService evidence, IMetricCalculationService metrics,
    IValoraIndexCalculationService indices, IInferenceEngine inference, IInsightGenerationService insights,
    IActionRecommendationService actions, IEvolutionService evolution, IHeatmapService heatmap, IRadarService radar,
    IBenchmarkService benchmark, IExecutiveReportService reports, IJourneyService journey, INotificationService notifications,
    IPlatformGovernanceService governance) : IOrganizationalIntelligencePipeline
{
    public Task<IntelligencePipelineResult> ProcessResponseAsync(IntelligenceProcessingContext c, CancellationToken ct) => Run(c with { Trigger = "response_received" }, false, ct);
    public Task<IntelligencePipelineResult> ProcessDiagnosisClosedAsync(IntelligenceProcessingContext c, CancellationToken ct) => Run(c with { Trigger = "diagnosis_closed" }, true, ct);
    public Task<IntelligencePipelineResult> ProcessActionAsync(IntelligenceProcessingContext c, bool completed, CancellationToken ct) => Run(c with { Trigger = completed ? "action_completed" : "action_created" }, false, ct);
    public Task<IntelligencePipelineResult> ProcessExecutiveReportAsync(IntelligenceProcessingContext c, CancellationToken ct) => Run(c with { Trigger = "executive_report_generated" }, true, ct);

    private async Task<IntelligencePipelineResult> Run(IntelligenceProcessingContext c, bool includeReport, CancellationToken ct)
    {
        var run = Guid.NewGuid(); var stages = new List<ProcessingStageResult>();
        var extracted = await evidence.ExtractAsync(c, ct); stages.Add(extracted); var ids = extracted.EvidenceIds;
        if (ids.Count > 0)
        {
            stages.Add(await metrics.CalculateAsync(c, ids, ct));
            stages.Add(await indices.CalculateAsync(c, ids, ct));
            stages.Add(await inference.InferAsync(c, ids, ct));
            stages.Add(await insights.GenerateAsync(c, ids, ct));
            stages.Add(await heatmap.RefreshAsync(c, ids, ct));
            stages.Add(await radar.RefreshAsync(c, ids, ct));
            stages.Add(await evolution.RefreshAsync(c, ids, ct));
            stages.Add(await actions.RecommendAsync(c, ids, ct));
            stages.Add(await benchmark.RefreshAsync(c, ids, ct));
            if (includeReport) stages.Add(await reports.SnapshotAsync(c, ids, ct));
        }
        await journey.RecordAsync(c, run, c.Trigger, "Pipeline de inteligência atualizado", extracted.Message, ct);
        await notifications.NotifyAsync(c, run, c.Trigger, "Inteligência organizacional atualizada", extracted.Message, ct);
        await governance.RecordAsync(c, run, c.Trigger, $"Pipeline {run} processado com {ids.Count} evidências vinculadas.", ct);
        return new(run, c.Trigger, stages, DateTime.UtcNow);
    }
}
