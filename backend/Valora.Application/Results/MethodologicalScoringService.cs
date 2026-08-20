namespace Valora.Application.Results;

public sealed record MethodologicalAnswer(
    Guid AnswerId, string QuestionCode, string DimensionCode, string ConceptCode,
    decimal? RawValue, decimal Minimum, decimal Maximum, decimal Weight = 1m,
    bool Reverse = false, bool IsQualitative = false, decimal? QualitativeNormalizedValue = null);

public sealed record ScoringEvidence(
    Guid AnswerId, string QuestionCode, string DimensionCode, string ConceptCode,
    decimal Intensity, string Polarity, decimal Confidence, string Interpretation);

public sealed record MethodologicalScoreGroup(string Code, decimal Score, decimal Weight, int EvidenceCount);

public sealed record MethodologicalScoreResult(
    decimal? OverallScore, string MaturityLevel, decimal Confidence,
    IReadOnlyList<MethodologicalScoreGroup> Concepts,
    IReadOnlyList<MethodologicalScoreGroup> Dimensions,
    IReadOnlyList<ScoringEvidence> Evidence,
    IReadOnlyList<string> IgnoredQuestions);

/// <summary>
/// Motor decimal, determinístico e independente de persistência da Metodologia Valora.
/// Recebe somente o snapshot metodológico travado no diagnóstico; nunca consulta a versão corrente.
/// </summary>
public sealed class MethodologicalScoringService
{
    public MethodologicalScoreResult Calculate(IEnumerable<MethodologicalAnswer> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ignored = new List<string>();
        var valid = new List<(MethodologicalAnswer Answer, decimal Score)>();

        foreach (var answer in input)
        {
            if (string.IsNullOrWhiteSpace(answer.QuestionCode) ||
                string.IsNullOrWhiteSpace(answer.DimensionCode) ||
                string.IsNullOrWhiteSpace(answer.ConceptCode) || answer.Weight <= 0m)
            {
                ignored.Add(answer.QuestionCode);
                continue;
            }

            decimal? normalized = answer.IsQualitative
                ? answer.QualitativeNormalizedValue
                : Normalize(answer.RawValue, answer.Minimum, answer.Maximum);
            if (normalized is null or < 0m or > 100m)
            {
                ignored.Add(answer.QuestionCode);
                continue;
            }

            valid.Add((answer, decimal.Round(answer.Reverse ? 100m - normalized.Value : normalized.Value, 4)));
        }

        var evidence = valid.Select(x => new ScoringEvidence(
            x.Answer.AnswerId, x.Answer.QuestionCode, x.Answer.DimensionCode, x.Answer.ConceptCode,
            x.Score, x.Score >= 60m ? "positive" : x.Score < 40m ? "negative" : "neutral",
            x.Answer.IsQualitative ? .70m : 1m,
            $"Resposta normalizada em {x.Score:0.##}/100 com peso {x.Answer.Weight:0.####}."))
            .ToArray();
        var concepts = Group(valid, x => x.Answer.ConceptCode);
        var dimensions = Group(valid, x => x.Answer.DimensionCode);
        var weight = valid.Sum(x => x.Answer.Weight);
        var overall = weight == 0m ? null : decimal.Round(valid.Sum(x => x.Score * x.Answer.Weight) / weight, 2);
        var confidence = input.Any() ? decimal.Round(valid.Count / (decimal)input.Count() * 100m, 2) : 0m;

        return new(overall, ResolveLevel(overall), confidence, concepts, dimensions, evidence, ignored);
    }

    private static decimal? Normalize(decimal? value, decimal minimum, decimal maximum)
        => value is null || maximum <= minimum || value < minimum || value > maximum
            ? null
            : (value.Value - minimum) / (maximum - minimum) * 100m;

    private static MethodologicalScoreGroup[] Group(
        IEnumerable<(MethodologicalAnswer Answer, decimal Score)> values,
        Func<(MethodologicalAnswer Answer, decimal Score), string> keySelector)
        => values.GroupBy(keySelector, StringComparer.OrdinalIgnoreCase).Select(group =>
        {
            var weight = group.Sum(x => x.Answer.Weight);
            return new MethodologicalScoreGroup(group.Key,
                decimal.Round(group.Sum(x => x.Score * x.Answer.Weight) / weight, 2), weight, group.Count());
        }).OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase).ToArray();

    private static string ResolveLevel(decimal? score) => score switch
    {
        null => "insufficient_evidence",
        < 20m => "initial",
        < 40m => "structuring",
        < 60m => "developing",
        < 75m => "consistent",
        < 90m => "mature",
        _ => "intelligent"
    };
}
