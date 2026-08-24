using System.Text.Json;

namespace Valora.Application.ValoraAi;

public enum AiRunStatus { Draft, PendingReview, Approved, Rejected, Published, Invalid, Failed, NotConfigured, LimitExceeded }
public enum AiConfidence { High, Medium, Low, Insufficient }

public sealed record ValoraEvidence(string Id, string Kind, string Summary, string? Dimension = null,
    string? Concept = null, decimal? Score = null, string? PersonName = null, string? PersonEmail = null);

public sealed record ValoraEvidencePack(Guid OrganizationId, string Organization, Guid DiagnosisId,
    string Methodology, string MethodologyVersion, bool Anonymous, IReadOnlyList<ValoraEvidence> Evidence,
    IReadOnlyDictionary<string, decimal> Scores, IReadOnlyList<string> Dimensions,
    IReadOnlyList<string> Concepts, IReadOnlyList<string> ConsolidatedAnswers, IReadOnlyList<string> History,
    IReadOnlyList<string> PreviousActionPlans, IReadOnlyList<string> PreviousReports,
    IReadOnlyList<string> PrivacyLimits);

public sealed record ValoraEvidenceSource(Guid OrganizationId, string Organization, Guid DiagnosisId,
    string Methodology, string MethodologyVersion, bool Anonymous, IReadOnlyList<ValoraEvidence> Evidence,
    IReadOnlyDictionary<string, decimal> Scores, IReadOnlyList<string> Dimensions,
    IReadOnlyList<string> Concepts, IReadOnlyList<string> ConsolidatedAnswers, IReadOnlyList<string> History,
    IReadOnlyList<string> PreviousActionPlans, IReadOnlyList<string> PreviousReports,
    IReadOnlyList<string> PrivacyLimits);

public sealed record ValoraPromptTemplate(string Code, string Name, int Version, string Objective,
    string SystemInstructions, string UserTemplate, string OutputSchema, string Status,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record ValoraRenderedPrompt(string Code, int Version, string System, string User, string OutputSchema);
public sealed record ValoraAiRequest(string Model, ValoraRenderedPrompt Prompt, string CorrelationId);
public sealed record ValoraAiProviderResult(string Content, int InputTokens, int OutputTokens, decimal EstimatedCost,
    string Provider, string Model);
public sealed record ValoraAiRun(Guid Id, Guid OrganizationId, Guid DiagnosisId, string PromptCode,
    int PromptVersion, string Provider, string Model, AiRunStatus Status, string CorrelationId,
    DateTime CreatedAt, string? Error = null);
public sealed record ValoraAiValidation(bool IsValid, IReadOnlyList<string> Violations);
public sealed record ValoraAiExecutionResult(ValoraAiRun Run, string? Output, ValoraAiValidation? Validation,
    string Message);
public sealed record AiUsageAllowance(bool Allowed, int Used, int Limit, decimal AlertThreshold = .8m);

public interface IValoraAiProvider
{
    bool IsConfigured { get; }
    Task<ValoraAiProviderResult> CompleteAsync(ValoraAiRequest request, CancellationToken ct);
}
public interface IValoraPromptRenderer { ValoraRenderedPrompt Render(ValoraPromptTemplate template, ValoraEvidencePack pack); }
public interface IValoraEvidencePackBuilder { ValoraEvidencePack Build(ValoraEvidenceSource source); }
public interface IValoraAiGuardrailService { ValoraAiValidation Validate(string output, ValoraEvidencePack pack); }
public interface IValoraAiRunRepository
{
    Task CreateAsync(ValoraAiRun run, ValoraEvidencePack input, CancellationToken ct);
    Task CompleteAsync(Guid runId, AiRunStatus status, string? output, ValoraAiValidation? validation,
        ValoraAiProviderResult? usage, string? error, CancellationToken ct);
    Task<AiUsageAllowance> CheckAllowanceAsync(Guid organizationId, CancellationToken ct);
    Task RecordReviewAsync(Guid runId, Guid reviewerId, AiRunStatus status, string? note, CancellationToken ct);
}
public interface IValoraAiOrchestrator
{
    Task<ValoraAiExecutionResult> ExecuteAsync(Guid organizationId, Guid diagnosisId, ValoraPromptTemplate prompt,
        ValoraEvidenceSource evidence, string correlationId, CancellationToken ct);
}
public interface IValoraAiReviewService
{
    Task ReviewAsync(Guid runId, Guid reviewerId, AiRunStatus current, AiRunStatus target, string? note, CancellationToken ct);
}
public interface IValoraInsightGenerator { Task<ValoraAiExecutionResult> GenerateAsync(Guid organizationId, Guid diagnosisId, ValoraEvidenceSource evidence, string correlationId, CancellationToken ct); }
public interface IValoraExecutiveReportGenerator { Task<ValoraAiExecutionResult> GenerateAsync(Guid organizationId, Guid diagnosisId, ValoraEvidenceSource evidence, string correlationId, CancellationToken ct); }
public interface IValoraActionRecommendationGenerator { Task<ValoraAiExecutionResult> GenerateAsync(Guid organizationId, Guid diagnosisId, ValoraEvidenceSource evidence, string correlationId, CancellationToken ct); }

public sealed record StructuredAiItem(string Title, string Interpretation, IReadOnlyList<string> EvidenceIds,
    string? Dimension, string? Concept, string? ProbableCause, string Impact, string Priority,
    string PriorityJustification, AiConfidence Confidence, string? Recommendation, string AnalysisLimitations,
    string? AssociatedRisk = null);
public sealed record ExecutiveInsight(StructuredAiItem Value);
public sealed record OrganizationalRisk(StructuredAiItem Value);
public sealed record OrganizationalOpportunity(StructuredAiItem Value);
public sealed record OrganizationalStrength(StructuredAiItem Value);
public sealed record OrganizationalFragility(StructuredAiItem Value);
public sealed record RecommendedAction(StructuredAiItem Value);
public sealed record ReportSection(StructuredAiItem Value);
public sealed record DimensionInterpretation(StructuredAiItem Value);
public sealed record EvolutionInterpretation(StructuredAiItem Value);

public static class ValoraConfidenceCalculator
{
    public static AiConfidence Calculate(int evidenceCount, int convergentSources) => (evidenceCount, convergentSources) switch
    {
        (0, _) => AiConfidence.Insufficient,
        (>= 3, >= 2) => AiConfidence.High,
        (>= 2, _) => AiConfidence.Medium,
        _ => AiConfidence.Low
    };
}
