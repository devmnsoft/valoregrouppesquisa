namespace Valora.Application.Results;

public sealed class ValoraInsightDevolutivaService
{
    public IReadOnlyList<string> RequiredSections => new[]
    {
        "Enquadramento geral sem adoçamento",
        "Leitura executiva da realidade",
        "Diagnóstico por dimensão",
        "Radar visual textual",
        "Benchmarking qualitativo",
        "Verdade estratégica central",
        "Risco se nada mudar",
        "Próximo nível",
        "Transição para solução",
        "CTA para falar com Valora"
    };

    public string Build(ValoraInsightResult result)
    {
        return string.Join("\n\n", RequiredSections.Select(section => section switch
        {
            "Enquadramento geral sem adoçamento" => $"{section}: {result.TotalScore}/125 — {result.Level}. A leitura é direta: o estágio atual mostra o quanto a empresa consegue sustentar crescimento sem depender de improviso.",
            "Leitura executiva da realidade" => $"{section}: {result.StrategicTruth}",
            "Diagnóstico por dimensão" => $"{section}: veja a distribuição de pontos por dimensão e trate o menor score como gargalo operacional, não como detalhe isolado.",
            "Radar visual textual" => $"{section}: {result.Radar}",
            "Benchmarking qualitativo" => $"{section}: comparada a operações maduras, a empresa precisa reduzir dependência de pessoas-chave, fortalecer ritos de decisão e transformar resultado em rotina previsível.",
            "Verdade estratégica central" => $"{section}: {result.StrategicTruth}",
            "Risco se nada mudar" => $"{section}: {result.Risk}",
            "Próximo nível" => $"{section}: {result.NextLevel}",
            "Transição para solução" => $"{section}: o diagnóstico encerra a fase de percepção e abre a fase de decisão; o próximo passo é organizar prioridades, responsáveis e ritmo de execução.",
            _ => $"{section}: fale com a Valora Group pelo WhatsApp para estruturar o plano de evolução. CTA: WhatsApp Valora."
        }));
    }
}
