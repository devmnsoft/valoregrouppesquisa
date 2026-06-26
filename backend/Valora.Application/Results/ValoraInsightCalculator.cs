namespace Valora.Application.Results;
public sealed class ValoraInsightCalculator
{
    public ValoraInsightResult Calculate(IEnumerable<AnswerScore> answers)
    {
        var byDimension = answers.GroupBy(a => a.Dimension).ToDictionary(g => g.Key, g => g.Sum(x => x.Score));
        var total = byDimension.Values.Sum();
        var level = total switch
        {
            >= 25 and <= 55 => "Crítico",
            >= 56 and <= 85 => "Em estruturação",
            >= 86 and <= 110 => "Estruturada",
            >= 111 and <= 125 => "Alta maturidade",
            < 25 => "Crítico",
            _ => "Alta maturidade"
        };
        return new ValoraInsightResult(
            total,
            125,
            level,
            byDimension,
            "Verdade estratégica calculada no backend como fonte final da maturidade Valora Insight™.",
            level == "Crítico" ? "Risco alto se nada mudar." : "Risco controlado com acompanhamento contínuo.",
            level == "Alta maturidade" ? "Sustentar excelência e escalar governança." : "Avançar para a próxima faixa oficial da régua Valora Insight™.");
    }
}
