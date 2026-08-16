namespace Valora.Application.OrganizationalIntelligence;

public static class IntelligenceProcessingStatus
{
    public const string Pending = "pending", Running = "running", Completed = "completed", Failed = "failed",
        RetryScheduled = "retry_scheduled", Cancelled = "cancelled", Skipped = "skipped", InsufficientEvidence = "insufficient_evidence";
}

public sealed record IntelligenceProcessingJob(Guid Id, Guid OrganizationId, Guid? SurveyId, Guid? ResponseId,
    Guid? FormId, Guid? SourceEntityId, string Trigger, string Status, int Priority, int Attempts, int MaxAttempts,
    DateTime ScheduledAt, DateTime? StartedAt, DateTime? CompletedAt, DateTime? FailedAt, DateTime? NextAttemptAt,
    string? LockedBy, string? ErrorCode, string? ErrorMessage, string? CorrelationId, DateTime CreatedAt);
public sealed record IntelligenceStageRun(Guid Id, Guid JobId, Guid RunId, string Stage, string Status, int Records,
    bool SufficientEvidence, string Message, DateTime StartedAt, DateTime? CompletedAt, long? DurationMs,
    string? ErrorCode, string? ErrorMessage, IReadOnlyList<Guid> EvidenceIds);
public sealed record IntelligenceJobFilter(string? Status = null, Guid? SurveyId = null, Guid? ResponseId = null,
    string? Trigger = null, int Page = 1, int PageSize = 50);
public sealed record IntelligenceProcessingSummary(int Pending, int Running, int CompletedToday, int Failed,
    int RetryScheduled, int InsufficientEvidence, decimal AverageDurationMs, string? MostFailedStage,
    DateTime? LastResponseProcessedAt, DateTime? LastDiagnosisProcessedAt);
public sealed record IntelligenceJobDetails(IntelligenceProcessingJob Job, IReadOnlyList<IntelligenceStageRun> Stages);

public interface IIntelligenceProcessingJobRepository
{
    Task<Guid> EnqueueAsync(IntelligenceProcessingContext context, int maxAttempts, string correlationId, CancellationToken ct);
    Task<IReadOnlyList<IntelligenceProcessingJob>> GetPendingJobsAsync(int take, CancellationToken ct);
    Task<bool> LockJobAsync(Guid jobId, string workerId, CancellationToken ct);
    Task MarkRunningAsync(Guid jobId, Guid runId, CancellationToken ct);
    Task MarkCompletedAsync(Guid jobId, string status, CancellationToken ct);
    Task MarkFailedAsync(Guid jobId, string code, string message, CancellationToken ct);
    Task ScheduleRetryAsync(Guid jobId, DateTime nextAttemptAt, string code, string message, CancellationToken ct);
    Task<bool> CancelAsync(Guid organizationId, Guid jobId, Guid? userId, CancellationToken ct);
    Task<Guid?> ReprocessAsync(Guid organizationId, Guid jobId, Guid? userId, string correlationId, CancellationToken ct);
    Task<IReadOnlyList<IntelligenceProcessingJob>> ListJobsAsync(Guid organizationId, IntelligenceJobFilter filter, CancellationToken ct);
    Task<IntelligenceProcessingJob?> GetJobAsync(Guid organizationId, Guid jobId, CancellationToken ct);
    Task<IReadOnlyList<IntelligenceStageRun>> ListStageRunsAsync(Guid organizationId, Guid jobId, CancellationToken ct);
    Task<IntelligenceProcessingSummary> GetSummaryAsync(Guid organizationId, CancellationToken ct);
    Task LogStageAsync(Guid organizationId, Guid jobId, Guid runId, ProcessingStageResult stage, string status, DateTime startedAt, DateTime completedAt, CancellationToken ct);
}

public interface IIntelligenceProcessingJobService
{
    Task<Guid> EnqueueResponseProcessingAsync(IntelligenceProcessingContext context, string correlationId, CancellationToken ct);
    Task<Guid> EnqueueDiagnosisClosedProcessingAsync(IntelligenceProcessingContext context, string correlationId, CancellationToken ct);
    Task<Guid> EnqueueActionProcessingAsync(IntelligenceProcessingContext context, string correlationId, CancellationToken ct);
    Task<Guid> EnqueueExecutiveReportProcessingAsync(IntelligenceProcessingContext context, string correlationId, CancellationToken ct);
    Task<Guid> EnqueueManualRecalculationAsync(IntelligenceProcessingContext context, string correlationId, CancellationToken ct);
    Task<IReadOnlyList<IntelligenceProcessingJob>> GetPendingJobsAsync(int take, CancellationToken ct);
    Task<IReadOnlyList<IntelligenceProcessingJob>> ListJobsAsync(Guid organizationId, IntelligenceJobFilter filter, CancellationToken ct);
    Task<IntelligenceJobDetails?> GetJobDetailsAsync(Guid organizationId, Guid jobId, CancellationToken ct);
    Task<IReadOnlyList<IntelligenceStageRun>> ListStageRunsAsync(Guid organizationId, Guid jobId, CancellationToken ct);
    Task<IntelligenceProcessingSummary> GetSummaryAsync(Guid organizationId, CancellationToken ct);
    Task<bool> CancelAsync(Guid organizationId, Guid jobId, Guid? userId, CancellationToken ct);
    Task<Guid?> ReprocessAsync(Guid organizationId, Guid jobId, Guid? userId, string correlationId, CancellationToken ct);
}
public interface IIntelligenceProcessingOrchestrator { Task ProcessAsync(IntelligenceProcessingJob job, CancellationToken ct); }
public interface IIntelligenceStageLogger { Task LogAsync(Guid organizationId, Guid jobId, Guid runId, ProcessingStageResult result, string status, DateTime startedAt, DateTime completedAt, CancellationToken ct); }
public interface IIntelligenceReprocessService { Task<Guid?> ReprocessAsync(Guid organizationId, Guid jobId, Guid? userId, string correlationId, CancellationToken ct); }
public interface IIntelligenceProcessingWorker { }
