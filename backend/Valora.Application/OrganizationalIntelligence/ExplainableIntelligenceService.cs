using System.Security.Cryptography;
using System.Text;
using Valora.Application.Knowledge;
using Valora.Application.Methodology;
using Valora.Application.ValoraAi;

namespace Valora.Application.OrganizationalIntelligence;

public sealed class ExplainableIntelligenceService(
    IValoraAiInsightRepository insights,
    IMethodologyRepository methodology,
    IKnowledgeRepository knowledge) : IExplainableIntelligenceService {

    private const string OfficialMethodologyVersion = "Valora Insight™ v2.0 - Metodologia Oficial";
    private const string AssociationNotice = "A Metodologia Valora™ estabelece separação estrita entre correlação e causalidade comprovada. A associação estatística ou de padrões entre os indicadores observados indica vulnerabilidade sistêmica provável, mas não comprova causa isolada sem validação contextual de campo.";
    private const string HumanSupervisionNotice = "As recomendações geradas por IA são hipóteses orientadoras e não constituem decisão executiva compulsória. O plano deve ser avaliado, ajustado e aprovado por liderança humana no Valora Action™.";
    private const string MethodologyBoundariesNotice = "Os conceitos metodológicos oficiais e termos do Dicionário Cognitivo são imutáveis e protegidos contra alterações por inferência de IA.";

    public async Task<ValoraExplainableAnalysis?> ExplainInsightAsync(
        Guid organizationId,
        Guid userId,
        Guid insightId,
        bool organizationWide,
        CancellationToken ct) {

        var detail = await insights.DetailsAsync(organizationId, userId, insightId, organizationWide, ct);
        if (detail is null) return null;

        var x = detail.Insight;
        var evidenceItems = detail.Evidence.Select(e => new ExplainableEvidenceItem(
            e.Id,
            e.Summary,
            e.Type,
            e.SourceType,
            e.Dimension,
            null,
            1.0m,
            e.CreatedAt
        )).ToList();

        var evidenceCount = evidenceItems.Count;
        var distinctSources = evidenceItems.Select(e => e.SourceType).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var meetsConvergence = evidenceCount >= 3 && distinctSources >= 2;

        var confidenceLevel = (evidenceCount, distinctSources) switch {
            ( <= 0, _) => "Insuficiente",
            ( < 3, _) => "Baixa",
            ( >= 3, < 2) => "Baixa", // Múltiplas evidências da mesma fonte não configuram convergência independente
            ( >= 5, >= 3) => "Alta",
            _ => "Moderada" // count >= 3 && distinctSources >= 2
        };

        var rule = (evidenceCount, distinctSources) switch {
            ( >= 3, >= 2) => $"Atende ao critério metodológico de convergência com {evidenceCount} evidências identificadas em {distinctSources} fontes independentes.",
            ( >= 3, < 2) => $"Foram identificadas {evidenceCount} evidências, porém restritas a 1 única fonte ({evidenceItems.FirstOrDefault()?.SourceType ?? "fonte única"}). A Metodologia Valora™ exige ao menos 2 fontes independentes para confirmação convergente.",
            (0, _) => "Nenhuma evidência empírica disponível para esta análise.",
            _ => $"A Metodologia Valora™ exige ao menos 3 evidências convergentes para conclusões de média/alta certeza. Foram identificadas apenas {evidenceCount} evidência(s); aguarda ampliação amostral."
        };

        // Link with Methodology Concepts & Cognitive Dictionary
        var dimensionName = x.RelatedDimension ?? "Geral";
        var concepts = await methodology.ListConceptsAsync(dimensionName, null, ct);
        var primaryConcept = concepts.FirstOrDefault();

        IReadOnlyList<string> cascading = [];
        if (primaryConcept is not null) {
            var relations = await methodology.ListRelationsAsync(primaryConcept.Code, ct);
            cascading = relations.Select(r => $"{r.TargetName} ({r.RelationType}, peso {r.InfluenceWeight:0.#})").ToList();
        }

        var terms = await knowledge.TermsAsync(organizationId, ct);
        var matchingTerm = terms.FirstOrDefault(t =>
            string.Equals(t.Name, dimensionName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.Code, dimensionName, StringComparison.OrdinalIgnoreCase));

        var capabilities = new List<ExplainableRelatedCapability> {
            new(
                DimensionCode: dimensionName.ToLowerInvariant().Replace(" ", "_"),
                DimensionName: dimensionName,
                ConceptCode: primaryConcept?.Code ?? dimensionName.ToLowerInvariant(),
                ConceptName: primaryConcept?.Name ?? dimensionName,
                MethodologicalDefinition: primaryConcept?.Definition ?? matchingTerm?.Definition ?? "Capacidade organizacional estrutural monitorada pelo modelo Valora™.",
                CognitiveDictionaryTerm: matchingTerm?.Name,
                Pillar: primaryConcept?.Pillar ?? "Inteligência Organizacional",
                CascadingInfluences: cascading
            )
        };

        var canonicalData = $"{x.Id:N}:{evidenceCount}:{string.Join(";", evidenceItems.Select(e => e.Id))}:{dimensionName}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalData))).ToLowerInvariant();

        return new ValoraExplainableAnalysis(
            AnalysisId: x.Id,
            Title: x.Title,
            WhatWasObserved: string.IsNullOrWhiteSpace(x.EvidenceSummary) ? x.Summary : x.EvidenceSummary,
            EvidenceConsidered: evidenceItems,
            RelatedCapabilities: capabilities,
            Confidence: new(
                Level: confidenceLevel,
                EvidenceCount: evidenceCount,
                ConvergentSourcesCount: distinctSources,
                MeetsMinimumConvergentRule: meetsConvergence,
                MethodologicalRule: rule
            ),
            Limitations: new(
                AssociationVsCausalityNotice: AssociationNotice,
                SampleScope: "Amostra restrita aos ciclos e instrumentos diagnósticos formalmente autorizados.",
                MethodologyBoundaries: MethodologyBoundariesNotice,
                RequiresAdditionalEvidence: !meetsConvergence
            ),
            Consequence: new(
                SystemicRisk: x.Limitation ?? $"Comprometimento de consistência na capacidade {dimensionName}.",
                ImpactSeverity: x.Severity ?? "Média",
                VulnerableCapabilities: cascading.Count > 0 ? cascading : [dimensionName]
            ),
            Evolution: new(
                RecommendedAction: x.Recommendation,
                TargetCapability: dimensionName,
                SuggestedTimeline: "Ciclo de 30 a 60 dias para implementação e reavaliação metodológica",
                HumanDecisionNotice: HumanSupervisionNotice
            ),
            MethodologyVersion: OfficialMethodologyVersion,
            DataHash: hash,
            GeneratedAt: x.CreatedAt
        );
    }

    public ValoraExplainableAnalysis ExplainInference(InferenceRequest request, InferenceResultDto result) {
        var count = result.Evidence.Count;
        var distinctSources = result.Evidence.Select(e => e.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var meetsConvergence = count >= 3 && distinctSources >= 2;

        var evidenceItems = result.Evidence.Select(e => new ExplainableEvidenceItem(
            Guid.NewGuid(),
            e.Description,
            "inference_evidence",
            e.Source,
            e.Code,
            e.Strength,
            1.0m,
            e.ObservedAt.DateTime
        )).ToList();

        var capabilities = result.Systems.Select(s => new ExplainableRelatedCapability(
            DimensionCode: s.ToLowerInvariant().Replace(" ", "_"),
            DimensionName: s,
            ConceptCode: s.ToLowerInvariant(),
            ConceptName: s,
            MethodologicalDefinition: $"Sistema organizacional e capacidade operacional {s}.",
            CognitiveDictionaryTerm: null,
            Pillar: "Sistemas Organizacionais",
            CascadingInfluences: []
        )).ToList();

        return new ValoraExplainableAnalysis(
            AnalysisId: Guid.NewGuid(),
            Title: result.Symptom,
            WhatWasObserved: $"Sintoma observado: {result.Symptom}. {result.Impact}",
            EvidenceConsidered: evidenceItems,
            RelatedCapabilities: capabilities,
            Confidence: new(
                Level: meetsConvergence ? result.Confidence : "Baixa",
                EvidenceCount: count,
                ConvergentSourcesCount: distinctSources,
                MeetsMinimumConvergentRule: meetsConvergence,
                MethodologicalRule: meetsConvergence
                    ? $"Atende à regra metodológica oficial com {count} evidências convergentes e {distinctSources} fontes independentes."
                    : "Inferência não conclusiva. A Metodologia Valora™ exige ao menos 3 evidências convergentes e ao menos 2 fontes independentes para confirmar correlações causais."
            ),
            Limitations: new(
                AssociationVsCausalityNotice: AssociationNotice,
                SampleScope: "Inferência algorítmica sobre eventos e evidências submetidos.",
                MethodologyBoundaries: MethodologyBoundariesNotice,
                RequiresAdditionalEvidence: !meetsConvergence
            ),
            Consequence: new(
                SystemicRisk: result.ProbableCause ?? "Causa não conclusiva devido à amostragem insuficiente.",
                ImpactSeverity: result.Priority,
                VulnerableCapabilities: result.Systems
            ),
            Evolution: new(
                RecommendedAction: result.NextStep,
                TargetCapability: string.Join(", ", result.Systems),
                SuggestedTimeline: "Intervenção prioritária",
                HumanDecisionNotice: HumanSupervisionNotice
            ),
            MethodologyVersion: result.MethodologyVersion ?? OfficialMethodologyVersion,
            DataHash: result.DataHash,
            GeneratedAt: DateTime.UtcNow
        );
    }

    public ValoraExplainableAnalysis ExplainExecutiveInsight(ExecutiveInsight insight, IReadOnlyList<DiagnosisEvidence> evidencePool) {
        var relevant = evidencePool.Where(e => insight.Evidence.Contains(e.Id)).ToList();
        var count = relevant.Count;
        var distinctSources = relevant.Select(e => e.Concept).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var meetsConvergence = count >= 3 && distinctSources >= 2;

        var items = relevant.Select(e => new ExplainableEvidenceItem(
            e.Id,
            e.Description,
            "diagnosis_evidence",
            e.Concept,
            e.Dimension,
            e.NormalizedScore,
            e.Weight,
            e.CreatedAt
        )).ToList();

        var canonical = $"{insight.Id:N}:{count}:{insight.Dimension}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();

        return new ValoraExplainableAnalysis(
            AnalysisId: insight.Id,
            Title: $"Leitura Executiva: {insight.Dimension}",
            WhatWasObserved: insight.Observation,
            EvidenceConsidered: items,
            RelatedCapabilities: [
                new(
                    DimensionCode: insight.Dimension.ToLowerInvariant().Replace(" ", "_"),
                    DimensionName: insight.Dimension,
                    ConceptCode: insight.Concept,
                    ConceptName: insight.Concept,
                    MethodologicalDefinition: insight.Interpretation,
                    CognitiveDictionaryTerm: insight.Concept,
                    Pillar: "Dimensões Oficiais Valora™",
                    CascadingInfluences: []
                )
            ],
            Confidence: new(
                Level: meetsConvergence
                    ? (insight.ConfidenceLevel == "high" ? "Alta" : "Moderada")
                    : "Baixa",
                EvidenceCount: count,
                ConvergentSourcesCount: distinctSources,
                MeetsMinimumConvergentRule: meetsConvergence,
                MethodologicalRule: meetsConvergence
                    ? $"Evidências convergentes suficientes ({count} itens rastreados em {distinctSources} fontes independentes)."
                    : $"Amostra com {count} evidências ({distinctSources} fontes). A Metodologia Valora™ exige ao menos 3 evidências convergentes e 2 fontes independentes; aguarda maior sustentação metodológica."
            ),
            Limitations: new(
                AssociationVsCausalityNotice: AssociationNotice,
                SampleScope: "Diagnóstico organizacional quantitativo e qualitativo persistido.",
                MethodologyBoundaries: MethodologyBoundariesNotice,
                RequiresAdditionalEvidence: !meetsConvergence
            ),
            Consequence: new(
                SystemicRisk: insight.Risk,
                ImpactSeverity: insight.Priority,
                VulnerableCapabilities: [insight.Dimension]
            ),
            Evolution: new(
                RecommendedAction: insight.Recommendation,
                TargetCapability: insight.Dimension,
                SuggestedTimeline: "Plano estratégico de evolução",
                HumanDecisionNotice: HumanSupervisionNotice
            ),
            MethodologyVersion: OfficialMethodologyVersion,
            DataHash: hash,
            GeneratedAt: insight.CreatedAt
        );
    }
}
