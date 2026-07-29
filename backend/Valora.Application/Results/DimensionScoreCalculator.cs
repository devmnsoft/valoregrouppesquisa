namespace Valora.Application.Results;

public sealed class DimensionScoreCalculator
{
    public IReadOnlyList<DimensionScoreOutput> Calculate(IEnumerable<(string Dimension, decimal Raw, decimal Max)> scores) => scores
        .GroupBy(s => string.IsNullOrWhiteSpace(s.Dimension) ? "Geral" : s.Dimension)
        .Select(g =>
        {
            var raw = g.Sum(x => x.Raw);
            var max = g.Sum(x => x.Max);
            var normalized = max <= 0 ? 0 : Math.Clamp(raw / max * 5, 0, 5);
            return new DimensionScoreOutput(g.Key, raw, max, normalized / 5 * 100, normalized);
        })
        .ToArray();
}
