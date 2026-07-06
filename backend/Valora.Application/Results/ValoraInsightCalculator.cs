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
        var weakest = byDimension.OrderBy(x => x.Value).FirstOrDefault();
        var strongest = byDimension.OrderByDescending(x => x.Value).FirstOrDefault();
        var radar = string.Join(" | ", byDimension.OrderBy(x => x.Key).Select(x => $"{x.Key}: {x.Value}/25"));
        var truth = $"Verdade estratégica central: o crescimento fica limitado por {weakest.Key ?? "dimensão crítica"} enquanto a empresa não transformar intenção em rotina de gestão.";
        var risk = "Risco se nada mudar: nos próximos 6 a 18 meses, a tendência é mais dependência de pessoas-chave, retrabalho, lentidão decisória e crescimento com desgaste.";
        var next = level == "Alta maturidade"
            ? $"Próximo nível: proteger {strongest.Key ?? "as fortalezas"}, reduzir variações entre áreas e escalar governança sem perder velocidade."
            : $"Próximo nível: priorizar {weakest.Key ?? "o gargalo principal"}, definir responsáveis e criar cadência de acompanhamento.";
        return new ValoraInsightResult(total, 125, level, radar, truth, risk, next);
    }
}
