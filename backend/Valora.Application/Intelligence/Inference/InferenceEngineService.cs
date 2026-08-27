namespace Valora.Application.Intelligence;

public sealed class InferenceEngineService
{
    public const int MinimumConvergentEvidence = 3;

    public IReadOnlyList<IntelligenceInference> Infer(IEnumerable<IntelligenceEvidence> evidence) => evidence
        .GroupBy(x => string.IsNullOrWhiteSpace(x.Dimension) ? x.Concept : x.Dimension, StringComparer.OrdinalIgnoreCase)
        .Select(group =>
        {
            var items = group.ToList();
            var strong = items.Select(x => x.SourceType).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2
                && items.Count >= MinimumConvergentEvidence;
            var average = items.Where(x => x.Intensity.HasValue).Select(x => x.Intensity!.Value).DefaultIfEmpty().Average();
            return new IntelligenceInference(
                $"Leitura de {group.Key}",
                strong ? $"Há convergência de {items.Count} evidências em {group.Key}." : $"Há somente {items.Count} evidência(s) em {group.Key}; a leitura permanece inconclusiva.",
                strong ? $"Hipótese a validar: práticas associadas a {group.Key} podem estar inconsistentes." : "Causa não determinada por insuficiência de evidências convergentes.",
                strong ? $"Impacto potencial na consistência organizacional de {group.Key}; requer validação humana." : "Impacto não determinado.",
                strong && average > 0 ? "moderate" : "insufficient", items.Select(x => x.Id).ToList(), strong);
        }).ToList();
}
