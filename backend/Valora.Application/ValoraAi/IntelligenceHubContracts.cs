namespace Valora.Application.ValoraAi;

public static class AiInsightStatuses
{
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
    string Recommendation, string Status, DateTime CreatedAt);
public sealed record AiInsightDraft(string InsightType, string Title, string Summary, IReadOnlyList<Guid> EvidenceIds,
    string? RelatedDimension, string? RelatedIndexCode, string Severity, string Priority, string ConfidenceLevel,
    string? Limitation, string Recommendation);
public sealed record AiReviewCommand(Guid OrganizationId, Guid InsightId, Guid ReviewerId, string Decision, string? Reason);

public interface IValoraAiEvidenceRepository
{
    Task<AiEvidencePack> BuildAsync(AiRunContext context, CancellationToken ct);
    Task SaveAsync(AiEvidencePack pack, Guid aiRunId, CancellationToken ct);
}
public interface IValoraAiInsightRepository
{
    Task<AiInsight?> GetAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<AiInsight>> ListAsync(Guid organizationId, string? status, CancellationToken ct);
    Task<Guid> CreateAsync(AiRunContext context, Guid runId, AiInsightDraft insight, CancellationToken ct);
    Task SetStatusAsync(Guid organizationId, Guid id, string status, Guid userId, CancellationToken ct);
}
public interface IValoraAiReviewRepository { Task RecordAsync(AiReviewCommand command, CancellationToken ct); }
public interface IValoraAiFeedbackRepository { Task RecordAsync(Guid organizationId, Guid insightId, Guid runId, Guid userId, string type, string reason, CancellationToken ct); }

public interface IAiGuardrailValidationService { ValoraAiValidation Validate(string output, ValoraEvidencePack evidence); }
public interface IEvidencePackBuilderService { Task<AiEvidencePack> BuildAsync(AiRunContext context, CancellationToken ct); }
public interface IInsightGenerationService { Task<ValoraAiExecutionResult> GenerateAsync(AiRunContext context, AiEvidencePack evidence, CancellationToken ct); }
public interface IAiFeedbackService { Task RecordRejectionAsync(AiReviewCommand command, Guid runId, CancellationToken ct); }

public static class AiInsufficientEvidence
{
    public const string Message = "As informações disponíveis ainda não permitem concluir esta análise com segurança.";
}
