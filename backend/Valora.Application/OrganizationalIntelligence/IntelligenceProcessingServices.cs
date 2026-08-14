namespace Valora.Application.OrganizationalIntelligence;

public sealed class IntelligenceProcessingJobService(IIntelligenceProcessingJobRepository repository) : IIntelligenceProcessingJobService, IIntelligenceReprocessService
{
    private Task<Guid> Enqueue(IntelligenceProcessingContext c, string trigger, string correlationId, CancellationToken ct) => repository.EnqueueAsync(c with { Trigger = trigger }, 3, correlationId, ct);
    public Task<Guid> EnqueueResponseProcessingAsync(IntelligenceProcessingContext c, string id, CancellationToken ct) => Enqueue(c, "response_received", id, ct);
    public Task<Guid> EnqueueDiagnosisClosedProcessingAsync(IntelligenceProcessingContext c, string id, CancellationToken ct) => Enqueue(c, "diagnosis_closed", id, ct);
    public Task<Guid> EnqueueActionProcessingAsync(IntelligenceProcessingContext c, string id, CancellationToken ct) => Enqueue(c, "action_changed", id, ct);
    public Task<Guid> EnqueueExecutiveReportProcessingAsync(IntelligenceProcessingContext c, string id, CancellationToken ct) => Enqueue(c, "executive_report", id, ct);
    public Task<Guid> EnqueueManualRecalculationAsync(IntelligenceProcessingContext c, string id, CancellationToken ct) => Enqueue(c, "manual_recalculation", id, ct);
    public Task<IReadOnlyList<IntelligenceProcessingJob>> GetPendingJobsAsync(int take, CancellationToken ct) => repository.GetPendingJobsAsync(take, ct);
    public Task<IReadOnlyList<IntelligenceProcessingJob>> ListJobsAsync(Guid o, IntelligenceJobFilter f, CancellationToken ct) => repository.ListJobsAsync(o, f, ct);
    public async Task<IntelligenceJobDetails?> GetJobDetailsAsync(Guid o, Guid id, CancellationToken ct) { var job = await repository.GetJobAsync(o,id,ct); return job is null ? null : new(job, await repository.ListStageRunsAsync(o,id,ct)); }
    public Task<IReadOnlyList<IntelligenceStageRun>> ListStageRunsAsync(Guid o, Guid id, CancellationToken ct) => repository.ListStageRunsAsync(o,id,ct);
    public Task<IntelligenceProcessingSummary> GetSummaryAsync(Guid o, CancellationToken ct) => repository.GetSummaryAsync(o,ct);
    public Task<bool> CancelAsync(Guid o, Guid id, Guid? u, CancellationToken ct) => repository.CancelAsync(o,id,u,ct);
    public Task<Guid?> ReprocessAsync(Guid o, Guid id, Guid? u, string correlationId, CancellationToken ct) => repository.ReprocessAsync(o,id,u,correlationId,ct);
}

public sealed class IntelligenceStageLogger(IIntelligenceProcessingJobRepository repository) : IIntelligenceStageLogger
{ public Task LogAsync(Guid o, Guid j, Guid r, ProcessingStageResult s, string status, DateTime from, DateTime to, CancellationToken ct) => repository.LogStageAsync(o,j,r,s,status,from,to,ct); }

public sealed class IntelligenceProcessingOrchestrator(IOrganizationalIntelligencePipeline pipeline,
    IIntelligenceProcessingJobRepository repository, IIntelligenceStageLogger stageLogger) : IIntelligenceProcessingOrchestrator
{
    public async Task ProcessAsync(IntelligenceProcessingJob job, CancellationToken ct)
    {
        var runId = Guid.NewGuid(); await repository.MarkRunningAsync(job.Id, runId, ct); var started = DateTime.UtcNow;
        try
        {
            var context = new IntelligenceProcessingContext(job.OrganizationId, job.SurveyId, job.ResponseId, job.FormId, SourceEntityId: job.SourceEntityId, Trigger: job.Trigger);
            var result = job.Trigger switch {
                "diagnosis_closed" => await pipeline.ProcessDiagnosisClosedAsync(context, ct),
                "executive_report" => await pipeline.ProcessExecutiveReportAsync(context, ct),
                "action_changed" => await pipeline.ProcessActionAsync(context, false, ct),
                _ => await pipeline.ProcessResponseAsync(context, ct) };
            foreach (var stage in result.Stages) { var status = stage.Records == 0 ? IntelligenceProcessingStatus.Skipped : stage.SufficientEvidence ? IntelligenceProcessingStatus.Completed : IntelligenceProcessingStatus.InsufficientEvidence; await stageLogger.LogAsync(job.OrganizationId,job.Id,runId,stage,status,started,DateTime.UtcNow,ct); }
            await repository.MarkCompletedAsync(job.Id, result.HasSufficientEvidence ? IntelligenceProcessingStatus.Completed : IntelligenceProcessingStatus.InsufficientEvidence, ct);
        }
        catch (Exception ex)
        {
            var message = "Não foi possível concluir esta etapa. A resposta original permanece preservada.";
            if (job.Attempts + 1 < job.MaxAttempts) await repository.ScheduleRetryAsync(job.Id, DateTime.UtcNow.AddSeconds(Math.Pow(2, job.Attempts + 1) * 15), "PIPELINE_STAGE_FAILED", message, ct);
            else await repository.MarkFailedAsync(job.Id, "PIPELINE_FAILED", message, ct);
            throw new InvalidOperationException(message, ex);
        }
    }
}
