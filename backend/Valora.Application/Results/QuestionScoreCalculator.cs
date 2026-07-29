namespace Valora.Application.Results;

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
