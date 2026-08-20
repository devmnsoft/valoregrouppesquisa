namespace Valora.Application.Results;

public sealed record StrategicMaturityAnswer(string QuestionCode, string DimensionCode, int Value);
public sealed record StrategicMaturityDimensionResult(string DimensionCode, string DimensionName, int Score, int MaxScore, decimal Average, string Level, string LevelLabel, string Interpretation, string PriorityFlag);
public sealed record StrategicMaturityRecommendation(string Title, string Text, IReadOnlyList<string> Steps, string Priority);
public sealed record StrategicMaturityResult(int MainTotal, int MainMinScore, int MainMaxScore, decimal MainAverage, string Level, string LevelLabel, string Meaning, IReadOnlyList<StrategicMaturityDimensionResult> Dimensions, int? EsgTotal, int EsgMaxScore, decimal? EsgAverage, StrategicMaturityRecommendation Recommendation, string BenchmarkMessage);

/// <summary>Regra determinística oficial do template VALORA_STRATEGIC_MATURITY_V1.</summary>
public sealed class StrategicMaturityScoringService
{
    private static readonly (string Code, string Name)[] Dimensions =
    [
        ("culture_purpose", "Cultura e Propósito"), ("management_governance", "Gestão e Governança"),
        ("leadership", "Liderança"), ("people_talents", "Pessoas e Talentos"),
        ("results_growth", "Resultados e Crescimento")
    ];

    public StrategicMaturityResult Calculate(IEnumerable<StrategicMaturityAnswer> input)
    {
        var answers = input.GroupBy(x => x.QuestionCode, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);
        if (answers.Values.Any(x => x.Value is < 1 or > 5)) throw new ArgumentException("Todas as respostas devem estar entre 1 e 5.", nameof(input));
        var missing = Enumerable.Range(1, 25).Select(Code).Where(code => !answers.ContainsKey(code)).ToArray();
        if (missing.Length != 0) throw new InvalidOperationException($"Respostas obrigatórias ausentes: {string.Join(", ", missing)}.");

        var mainTotal = Enumerable.Range(1, 25).Sum(i => answers[Code(i)].Value);
        var level = ResolveLevel(mainTotal);
        var dimensions = Dimensions.Select(d =>
        {
            var selected = answers.Values.Where(x => string.Equals(x.DimensionCode, d.Code, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (selected.Length != 5) throw new InvalidOperationException($"A dimensão {d.Name} deve conter exatamente 5 respostas.");
            var score = selected.Sum(x => x.Value);
            var dimensionLevel = ResolveLevelByDimension(score);
            return new StrategicMaturityDimensionResult(d.Code, d.Name, score, 25, decimal.Round(score / 5m, 1), dimensionLevel.Code, dimensionLevel.Label, dimensionLevel.Meaning, dimensionLevel.Priority);
        }).ToArray();

        var esg = new[] { Code(26), Code(27) }.Where(answers.ContainsKey).Select(code => answers[code].Value).ToArray();
        decimal? esgAverage = esg.Length == 0
            ? null
            : decimal.Round(esg.Sum(static value => (decimal)value) / esg.Length, 1);

        return new(mainTotal, 25, 125, decimal.Round(mainTotal / 25m, 1), level.Code, level.Label, level.Meaning, dimensions,
            esg.Length == 0 ? null : esg.Sum(), 10, esgAverage, Recommendation(level.Code),
            "Comparativo setorial ainda não informado para este diagnóstico.");
    }

    private static string Code(int number) => $"VALORA_MATURITY_Q{number:00}";
    private static (string Code, string Label, string Meaning) ResolveLevel(int score) => score switch
    {
        >= 25 and <= 55 => ("attention", "🔴 Atenção", "Fragilidades críticas"),
        >= 56 and <= 85 => ("evolution", "🟡 Evolução", "Fundamentos presentes, oportunidades de fortalecimento"),
        >= 86 and <= 110 => ("consistency", "🟢 Consistência", "Sistemas estruturados, operação segura"),
        >= 111 and <= 125 => ("excellence", "🔵 Excelência", "Maturidade elevada, geração sustentável de valor"),
        _ => throw new InvalidOperationException("Pontuação principal fora do intervalo oficial de 25 a 125.")
    };
    private static (string Code, string Label, string Meaning, string Priority) ResolveLevelByDimension(int score) => score switch
    {
        >= 5 and <= 11 => ("attention", "🔴 Atenção", "Fragilidades críticas", "high"),
        >= 12 and <= 17 => ("evolution", "🟡 Evolução", "Fundamentos presentes, oportunidades de fortalecimento", "medium"),
        >= 18 and <= 22 => ("consistency", "🟢 Consistência", "Sistemas estruturados, operação segura", "improvement"),
        >= 23 and <= 25 => ("excellence", "🔵 Excelência", "Maturidade elevada, geração sustentável de valor", "maintenance"),
        _ => throw new InvalidOperationException("Pontuação de dimensão fora do intervalo oficial de 5 a 25.")
    };
    private static StrategicMaturityRecommendation Recommendation(string level) => level switch
    {
        "attention" => new("Se você está em 🔴 Atenção", "Fragilidades críticas.", ["Defina 1 indicador chave, como meta de receita, NPS ou retenção de talentos.", "Realize 1 reunião de alinhamento com liderança para priorizar 1 fragilidade.", "Documente 1 processo crítico que está dependente de poucas pessoas."], "Estabilidade operacional + governança básica."),
        "evolution" => new("Se você está em 🟡 Evolução", "Fundamentos presentes, oportunidades de fortalecimento.", ["Fortalecer governança, especialmente os itens 6–10, criando processos claros e acompanhando indicadores.", "Criar plano de desenvolvimento de talentos, especialmente os itens 16–20, com oportunidades claras de crescimento.", "Estabelecer ciclo de feedback regular, especialmente os itens 11–15, fortalecendo a liderança no desenvolvimento das pessoas."], "Consistência procedural + fortalecimento de liderança."),
        "consistency" => new("Se você está em 🟢 Consistência", "Sistemas estruturados, operação segura.", ["Implementar métricas de inovação, como percentual de receita de novos produtos.", "Expandir programas de liderança, como mentoria, coaching e formação.", "Fazer benchmark com empresas de Excelência, comparando práticas."], "Inovação + diferenciação competitiva."),
        _ => new("Se você está em 🔵 Excelência", "Maturidade elevada, geração sustentável de valor.", ["Monitorar continuamente, repetindo o diagnóstico 1 vez por ano.", "Compartilhar práticas com o setor e mentorar outras empresas.", "Buscar certificação formal, como EFQM, ISO 9001 ou CMMI se for software."], "Manutenção + liderança de mercado.")
    };
}
