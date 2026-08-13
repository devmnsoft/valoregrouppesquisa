namespace Valora.Application.Results;

public sealed class SurveyResultCalculator
{
    private readonly QuestionScoreCalculator _question = new();
    private readonly DimensionScoreCalculator _dimension = new();
    private readonly ResultBandResolver _bands = new();

    public SurveyResultOutput Calculate(IEnumerable<SurveyQuestionInput> questions, IEnumerable<SurveyAnswerInput> answers)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(answers);

        // A client can retry/autosave the same question before submitting the final
        // payload. Treat the last occurrence as authoritative instead of allowing
        // ToDictionary to turn a recoverable request into a runtime exception.
        var answerMap = answers
            .Where(answer => !string.IsNullOrWhiteSpace(answer.QuestionId))
            .GroupBy(answer => answer.QuestionId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);
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
