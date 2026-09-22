using Valora.Application.ActionCenter;
using Valora.Application.Workspace;

namespace Valora.Application.ValoraAi;

public static class AiInsightStatuses {
    public const string Draft = "draft", Generated = "generated", PendingReview = "pending_review",
        Approved = "approved", Rejected = "rejected", ConvertedToAction = "converted_to_action",
        ConvertedToDecision = "converted_to_decision", Archived = "archived";
}

public sealed record AiRunContext(Guid OrganizationId, Guid DiagnosticId, Guid? ResultId,
    Guid? MethodologyVersionId, Guid RequestedByUserId, string RunType, string CorrelationId);
public sealed record AiEvidenceItem(Guid Id, string Type, string SourceType, Guid? SourceId, string Summary,
    string? Dimension, string? IndexCode, bool IsAggregate = true);
public sealed record AiEvidencePack(Guid Id, AiRunContext Context, IReadOnlyList<AiEvidenceItem> Items,
    IReadOnlyList<string> Limitations, DateTime CreatedAt);
public sealed record AiInsight(Guid Id, Guid OrganizationId, Guid DiagnosticId, Guid? ResultId, Guid AiRunId,
    string InsightType, string Title, string Summary, string EvidenceSummary, string? RelatedDimension,
    string? RelatedIndexCode, string Severity, string Priority, string ConfidenceLevel, string? Limitation,
    string Recommendation, string Status, DateTime CreatedAt, DateTime UpdatedAt, long ReviewVersion = 0,
    Guid? ReviewedByUserId = null, DateTime? ReviewedAt = null, Guid? LinkedPlanId = null);
public sealed record AiInsightListQuery(int Page = 1, int PageSize = 20, string? Search = null,
    string? Status = null, string? Priority = null, string? Dimension = null, Guid? DiagnosticId = null,
    bool? HasPlan = null, string? Sort = null) {
    public int ValidPage => Math.Clamp(Page, 1, 1000000);
    public int ValidPageSize => Math.Clamp(PageSize, 1, 50);
}
public sealed record AiInsightIndicators(int PendingReview, int ApprovedWithoutPlan, int WithPlansInExecution,
    int WithAllPlansClosed);
public sealed record AiInsightListItem(AiInsight Insight, string? DiagnosticName, int PlanCount,
    int ActivePlanCount, int ClosedPlanCount);
public sealed record AiDiagnosticOption(Guid Id, string Name);
public sealed record AiInsightListResult(PageResult<AiInsightListItem> Page, AiInsightIndicators Indicators,
    int AuthorizedTotal = 0, IReadOnlyList<AiDiagnosticOption>? Diagnostics = null);
public sealed record AiInsightEvidence(Guid Id, string Type, string SourceType, Guid? SourceId, string Summary,
    string? Dimension, string? IndexCode, DateTime CreatedAt, bool IsAvailable, bool IsRestricted = false);
public sealed record AiInsightDetails(AiInsight Insight, IReadOnlyList<AiInsightEvidence> Evidence,
    IReadOnlyList<ActionPlanDto> Plans);
public sealed record AiInsightDraft(string InsightType, string Title, string Summary, IReadOnlyList<Guid> EvidenceIds,
    string? RelatedDimension, string? RelatedIndexCode, string Severity, string Priority, string ConfidenceLevel,
    string? Limitation, string Recommendation);
public sealed record AiReviewCommand(Guid OrganizationId, Guid InsightId, Guid ReviewerId, string Decision, string? Reason,
    string? CommandKey = null, long ExpectedVersion = 0);
public enum AiReviewResult { Applied, Replayed, NotFound, Conflict }

public interface IValoraAiEvidenceRepository {
    Task<AiEvidencePack> BuildAsync(AiRunContext context, CancellationToken ct);
    Task SaveAsync(AiEvidencePack pack, Guid aiRunId, CancellationToken ct);
}
public interface IValoraAiInsightRepository {
    Task<AiInsight?> GetAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<AiInsight>> ListAsync(Guid organizationId, string? status, CancellationToken ct);
    Task<AiInsightListResult> ListAsync(Guid organizationId, Guid userId, bool organizationWide, AiInsightListQuery query, CancellationToken ct);
    Task<AiInsightDetails?> DetailsAsync(Guid organizationId, Guid userId, Guid id, bool organizationWide, CancellationToken ct);
    Task<Guid> CreateAsync(AiRunContext context, Guid runId, AiInsightDraft insight, CancellationToken ct);
}
public interface IValoraAiReviewRepository { Task<AiReviewResult> ApplyAsync(AiReviewCommand command, CancellationToken ct); }
public interface IValoraAiFeedbackRepository { Task RecordAsync(Guid organizationId, Guid insightId, Guid runId, Guid userId, string type, string reason, CancellationToken ct); }

public interface IAiGuardrailValidationService { ValoraAiValidation Validate(string output, ValoraEvidencePack evidence); }
public interface IEvidencePackBuilderService { Task<AiEvidencePack> BuildAsync(AiRunContext context, CancellationToken ct); }
public interface IInsightGenerationService { Task<ValoraAiExecutionResult> GenerateAsync(AiRunContext context, AiEvidencePack evidence, CancellationToken ct); }
public interface IAiFeedbackService { Task RecordRejectionAsync(AiReviewCommand command, Guid runId, CancellationToken ct); }

public static class AiInsufficientEvidence {
    public const string Message = "As informações disponíveis ainda não permitem concluir esta análise com segurança.";
}
