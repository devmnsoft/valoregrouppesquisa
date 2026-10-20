namespace Valora.Application.OrganizationalIntelligence;

public sealed record ExplainableEvidenceItem(
    Guid Id,
    string Summary,
    string Type,
    string SourceType,
    string? Dimension,
    decimal? NormalizedScore,
    decimal Weight,
    DateTime CreatedAt
);

public sealed record ExplainableRelatedCapability(
    string DimensionCode,
    string DimensionName,
    string? ConceptCode,
    string? ConceptName,
    string? MethodologicalDefinition,
    string? CognitiveDictionaryTerm,
    string? Pillar,
    IReadOnlyList<string> CascadingInfluences
);

public sealed record ExplainableConfidenceAssessment(
    string Level,
    int EvidenceCount,
    int ConvergentSourcesCount,
    bool MeetsMinimumConvergentRule,
    string MethodologicalRule
);

public sealed record ExplainableLimitations(
    string AssociationVsCausalityNotice,
    string SampleScope,
    string MethodologyBoundaries,
    bool RequiresAdditionalEvidence
);

public sealed record ExplainableConsequence(
    string SystemicRisk,
    string ImpactSeverity,
    IReadOnlyList<string> VulnerableCapabilities
);

public sealed record ExplainableEvolution(
    string RecommendedAction,
    string TargetCapability,
    string SuggestedTimeline,
    string HumanDecisionNotice
);

public sealed record ValoraExplainableAnalysis(
    Guid AnalysisId,
    string Title,
    string WhatWasObserved,
    IReadOnlyList<ExplainableEvidenceItem> EvidenceConsidered,
    IReadOnlyList<ExplainableRelatedCapability> RelatedCapabilities,
    ExplainableConfidenceAssessment Confidence,
    ExplainableLimitations Limitations,
    ExplainableConsequence Consequence,
    ExplainableEvolution Evolution,
    string MethodologyVersion,
    string DataHash,
    DateTime GeneratedAt
);

public interface IExplainableIntelligenceService {
    Task<ValoraExplainableAnalysis?> ExplainInsightAsync(Guid organizationId, Guid userId, Guid insightId, bool organizationWide, CancellationToken ct);
    ValoraExplainableAnalysis ExplainInference(Methodology.InferenceRequest request, Methodology.InferenceResultDto result);
    ValoraExplainableAnalysis ExplainExecutiveInsight(ExecutiveInsight insight, IReadOnlyList<DiagnosisEvidence> evidencePool);
}
