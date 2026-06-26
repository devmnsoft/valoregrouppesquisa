namespace Valora.Application.Results;
public sealed record AnswerScore(string Dimension, decimal Score);
public sealed record ValoraInsightResult(decimal TotalScore, decimal MaxScore, string Level, object Radar, string StrategicTruth, string Risk, string NextLevel);
public sealed class ValoraInsightCalculator
{
    public ValoraInsightResult Calculate(IEnumerable<AnswerScore> answers)
    {
        var byDimension = answers.GroupBy(a => a.Dimension).ToDictionary(g => g.Key, g => g.Sum(x => x.Score));
        var total = byDimension.Values.Sum();
        var level = total switch { < 50 => "critical", < 75 => "basic", < 100 => "evolving", < 115 => "advanced", _ => "excellent" };
        return new ValoraInsightResult(total, 125, level, byDimension, "Verdade estratégica calculada no backend.", level == "critical" ? "alto" : "controlado", "Próximo nível oficial conforme régua Valora Insight™.");
    }
}
