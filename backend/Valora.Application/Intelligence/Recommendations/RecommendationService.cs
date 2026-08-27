namespace Valora.Application.Intelligence;

public sealed class RecommendationService
{
    public IReadOnlyList<IntelligenceRecommendation> Build(IEnumerable<IntelligenceInference> inferences) => inferences
        .Where(x => x.IsConclusive)
        .Select(x => new IntelligenceRecommendation("high", $"Validar e evoluir {x.Title.Replace("Leitura de ", "")}",
            x.Impact, "medium", "Validar a hipótese com responsáveis, pactuar indicador, responsável, prazo e critério de conclusão.", x.EvidenceIds))
        .ToList();
}

public sealed class DecisionSupportService
{
    public IReadOnlyList<DecisionSuggestion> Build(IEnumerable<IntelligenceRecommendation> recommendations) => recommendations
        .Select(x => new DecisionSuggestion($"Priorizar {x.Title}", x.SuggestedAction,
            "Decidir sem validação humana pode tratar sintoma como causa.", x.ExpectedImpact, "suggested", x.EvidenceIds))
        .ToList();
}

public sealed class OrganizationalRiskService { }
public sealed class MaturityInterpretationService { }
public sealed class ExecutiveNarrativeService { }
