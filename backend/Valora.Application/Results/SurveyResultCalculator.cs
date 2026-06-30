namespace Valora.Application.Results;

public sealed record SurveyQuestionInput(string Id, string Type, string? Dimension, decimal Weight, decimal MaxScore, decimal? ScoreWhenFilled, IReadOnlyList<SurveyOptionInput> Options);
public sealed record SurveyOptionInput(string Id, decimal Score, bool Correct = false);
public sealed record SurveyAnswerInput(string QuestionId, object? Value);
public sealed record DimensionScoreOutput(string Dimension, decimal RawScore, decimal MaxScore, decimal Percentage, decimal Normalized5);
public sealed record SurveyResultOutput(decimal RawScore, decimal MaxScore, decimal Percentage, decimal Normalized5, string Band, string Recommendation, IReadOnlyList<DimensionScoreOutput> Dimensions);

public sealed class QuestionScoreCalculator
{
    public (decimal raw, decimal max) Calculate(SurveyQuestionInput question, object? answer)
    {
        var max = question.MaxScore > 0 ? question.MaxScore : 5;
        var raw = question.Type switch
        {
            "scale" => Math.Clamp(Convert.ToDecimal(answer ?? 0), 0, 5) / 5 * max,
            "single" => question.Options.FirstOrDefault(o => o.Id == Convert.ToString(answer))?.Score ?? 0,
            "singleCorrect" => question.Options.FirstOrDefault(o => o.Id == Convert.ToString(answer) && o.Correct) is null ? 0 : max,
            "multiple" => ScoreMultiple(question, answer),
            "text" or "shortText" or "longText" => string.IsNullOrWhiteSpace(Convert.ToString(answer)) ? 0 : question.ScoreWhenFilled ?? 0,
            _ => 0
        };
        var configuredMax = question.Type == "multiple" && question.MaxScore <= 0 ? question.Options.Where(o => o.Score > 0).Sum(o => o.Score) : max;
        return (raw * Math.Max(0, question.Weight), configuredMax * Math.Max(0, question.Weight));
    }

    private static decimal ScoreMultiple(SurveyQuestionInput question, object? answer)
    {
        var selected = answer as IEnumerable<string> ?? Convert.ToString(answer)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
        return question.Options.Where(o => selected.Contains(o.Id)).Sum(o => o.Score);
    }
}

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

public sealed class ResultBandResolver
{
    public (string band, string recommendation) Resolve(decimal normalized5) => normalized5 switch
    {
        < 2 => ("Crítico", "Priorizar plano de ação imediato e revisão dos processos essenciais."),
        < 3.5m => ("Em estruturação", "Consolidar governança, indicadores e automação dos pontos mais frágeis."),
        < 4.5m => ("Estruturada", "Aprimorar escala, padronização e acompanhamento por dimensão."),
        _ => ("Alta maturidade", "Sustentar excelência e expandir benchmark e melhoria contínua.")
    };
}

public sealed class SurveyResultCalculator
{
    private readonly QuestionScoreCalculator _question = new();
    private readonly DimensionScoreCalculator _dimension = new();
    private readonly ResultBandResolver _bands = new();

    public SurveyResultOutput Calculate(IEnumerable<SurveyQuestionInput> questions, IEnumerable<SurveyAnswerInput> answers)
    {
        var answerMap = answers.ToDictionary(a => a.QuestionId, a => a.Value);
        var scores = questions.Select(q =>
        {
            var (raw, max) = _question.Calculate(q, answerMap.GetValueOrDefault(q.Id));
            return (Dimension: q.Dimension ?? "Geral", Raw: raw, Max: max);
        }).ToArray();
        var rawScore = scores.Sum(s => s.Raw);
        var maxScore = scores.Sum(s => s.Max);
        var normalized5 = maxScore <= 0 ? 0 : Math.Clamp(rawScore / maxScore * 5, 0, 5);
        var (band, recommendation) = _bands.Resolve(normalized5);
        return new SurveyResultOutput(rawScore, maxScore, normalized5 / 5 * 100, normalized5, band, recommendation, _dimension.Calculate(scores));
    }
}
