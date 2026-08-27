using Valora.Application.OrganizationalIntelligence;

namespace Valora.Application.Intelligence;

public sealed record IntelligenceEvidence(Guid Id, string SourceType, string Dimension, string Concept,
    decimal? Intensity, decimal Reliability, string Description);
public sealed record IntelligenceInference(string Title, string Description, string ProbableCause,
    string Impact, string ConfidenceLevel, IReadOnlyList<Guid> EvidenceIds, bool IsConclusive);
public sealed record CausalRelation(string Symptom, string ProbableCause, string Impact, string ImpactArea,
    string ConfidenceLevel, IReadOnlyList<Guid> EvidenceIds);
public sealed record IntelligenceRecommendation(string Priority, string Title, string ExpectedImpact,
    string EstimatedEffort, string SuggestedAction, IReadOnlyList<Guid> EvidenceIds);
public sealed record DecisionSuggestion(string Title, string Rationale, string Risk, string ExpectedImpact,
    string Status, IReadOnlyList<Guid> EvidenceIds);
public sealed record IntelligenceAnalysis(Guid OrganizationId, DateTime GeneratedAt, string ConfidenceLevel,
    string Situation, string Limitation, IReadOnlyList<IntelligenceEvidence> Evidence,
    IReadOnlyList<IntelligenceInference> Inferences, IReadOnlyList<CausalRelation> CausalMap,
    IReadOnlyList<IntelligenceRecommendation> Recommendations, IReadOnlyList<DecisionSuggestion> Decisions);

public interface IIntelligenceAnalysisService
{
    Task<IntelligenceAnalysis> AnalyzeAsync(Guid organizationId, CancellationToken cancellationToken);
}

/// <summary>Evidence-first orchestration. It never upgrades a symptom to a cause without convergence.</summary>
public sealed class IntelligenceAnalysisService(
    IOrganizationalIntelligenceRepository repository,
    EvidenceMatrixService evidenceMatrix,
    InferenceEngineService inferenceEngine,
    CausalMapService causalMap,
    RecommendationService recommendations,
    DecisionSupportService decisions) : IIntelligenceAnalysisService
{
    public async Task<IntelligenceAnalysis> AnalyzeAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var source = await repository.ListEvidenceItemsAsync(organizationId, cancellationToken);
        var evidence = evidenceMatrix.Build(source);
        var inferences = inferenceEngine.Infer(evidence);
        var relations = causalMap.Build(inferences);
        var proposed = recommendations.Build(inferences);
        var suggestedDecisions = decisions.Build(proposed);
        var conclusive = inferences.Count(x => x.IsConclusive);
        var confidence = conclusive > 0 ? "moderate" : "insufficient";
        var situation = evidence.Count == 0
            ? "Ainda não há evidências organizacionais autorizadas para produzir uma leitura."
            : $"Foram consolidadas {evidence.Count} evidências reais; {conclusive} hipótese(s) atingiram a convergência metodológica.";
        var limitation = conclusive == 0
            ? "Evidência insuficiente: uma inferência forte exige ao menos três evidências convergentes. Nenhuma causa ou recomendação foi afirmada."
            : "Causas e impactos permanecem hipóteses para validação humana; a análise não expõe respostas individuais.";
        return new(organizationId, DateTime.UtcNow, confidence, situation, limitation, evidence, inferences, relations, proposed, suggestedDecisions);
    }
}
