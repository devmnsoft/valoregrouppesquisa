namespace Valora.Application.Intelligence;

public sealed class CausalMapService
{
    public IReadOnlyList<CausalRelation> Build(IEnumerable<IntelligenceInference> inferences) => inferences
        .Where(x => x.IsConclusive)
        .Select(x => new CausalRelation(x.Description, x.ProbableCause, x.Impact, x.Title.Replace("Leitura de ", ""), x.ConfidenceLevel, x.EvidenceIds))
        .ToList();
}
