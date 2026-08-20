namespace Valora.Application.OrganizationalIntelligence;

public static class ValoraOfficialDimensions
{
    public static readonly IReadOnlyList<string> All =
    [
        "Cultura Organizacional", "Governança Organizacional", "Liderança", "Pessoas", "Sistemas",
        "Clareza Sistêmica™", "Inteligência Organizacional", "Maturidade Organizacional",
        "Desenvolvimento Organizacional", "Sustentabilidade Organizacional"
    ];
}

public sealed record DiagnosisEvidence(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal? NormalizedScore, decimal Weight, string Description, DateTime CreatedAt,
    string? Group = null);
public sealed record MaturityDimensionScore(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal? Score, string MaturityLevel, string ConfidenceLevel,
    IReadOnlyList<Guid> Evidence, string Interpretation, string? Risk, string? Recommendation,
    string Priority, DateTime CreatedAt);
public sealed record EvidenceItem(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension, string Concept,
    decimal? Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string? Risk, string? Recommendation, string Priority, DateTime CreatedAt,
    string Description);
public sealed record OrganizationalRisk(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string Risk, string Recommendation, string Priority, DateTime CreatedAt);
public sealed record OrganizationalOpportunity(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string? Risk, string Recommendation, string Priority, DateTime CreatedAt);
public sealed record OrganizationalStrength(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string? Risk, string? Recommendation, string Priority, DateTime CreatedAt);
public sealed record OrganizationalFragility(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string Risk, string Recommendation, string Priority, DateTime CreatedAt);
public sealed record RecommendedPriority(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string Risk, string Recommendation, string Priority, DateTime CreatedAt);
public sealed record ExecutiveInsight(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Observation, string Interpretation, string Correlation, string ProbableCause,
    string OrganizationalImpact, string Risk, string Recommendation, string Priority, DateTime CreatedAt);
public sealed record ActionPlanItem(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string Risk, string Recommendation, string Priority, DateTime CreatedAt,
    string Objective, string Description, string RelatedCause, string? Owner, DateTime? DueAt,
    string SuccessIndicator, string Status, DateTime? CompletedAt, string? Notes);
public sealed record EvolutionSnapshot(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal? Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string? Risk, string? Recommendation, string Priority, DateTime CreatedAt,
    IReadOnlyList<MaturityDimensionScore> Dimensions);
public sealed record HeatmapCell(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal? Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string? Risk, string? Recommendation, string Priority, DateTime CreatedAt,
    string? Group, string Intensity);
public sealed record RadarDimension(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal? Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string? Risk, string? Recommendation, string Priority, DateTime CreatedAt,
    bool IsOfficial, string SystemicEffect);
public sealed record BenchmarkComparison(Guid Id, Guid OrganizationId, Guid? SurveyId, string Dimension,
    string Concept, decimal? Score, string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence,
    string Interpretation, string? Risk, string? Recommendation, string Priority, DateTime CreatedAt,
    string Scope, decimal? PreviousScore, decimal? Difference, int SampleSize);
public sealed record ExecutiveReportSection(string Code, string Title, string Content, IReadOnlyList<Guid> Evidence);
public sealed record ExecutiveReportViewModel(Guid Id, Guid OrganizationId, Guid? SurveyId, decimal? Score,
    string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence, string Interpretation,
    IReadOnlyList<ExecutiveReportSection> Sections, DateTime CreatedAt, string ExportStatus = "html_ready");
public sealed record OrganizationalDiagnosisSummary(Guid Id, Guid OrganizationId, Guid? SurveyId, decimal? Score,
    string MaturityLevel, string ConfidenceLevel, IReadOnlyList<Guid> Evidence, string Interpretation,
    IReadOnlyList<MaturityDimensionScore> Dimensions, IReadOnlyList<OrganizationalRisk> Risks,
    IReadOnlyList<OrganizationalOpportunity> Opportunities, IReadOnlyList<OrganizationalStrength> Strengths,
    IReadOnlyList<OrganizationalFragility> Fragilities, IReadOnlyList<RecommendedPriority> Priorities,
    IReadOnlyList<ExecutiveInsight> Insights, IReadOnlyList<ActionPlanItem> Actions,
    IReadOnlyList<HeatmapCell> Heatmap, IReadOnlyList<RadarDimension> Radar,
    ExecutiveReportViewModel Report, DateTime CreatedAt);

public interface IValoraIntelligenceEngine
{
    OrganizationalDiagnosisSummary Analyze(Guid organizationId, Guid? surveyId,
        IEnumerable<DiagnosisEvidence> evidence, DateTime? createdAt = null);
}

