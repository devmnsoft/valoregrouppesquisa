using Valora.Application.Results;

namespace Valora.Tests;

public sealed class ValoraInferenceMethodTests
{
    [Theory]
    [InlineData(0, "baixa")]
    [InlineData(2, "baixa")]
    [InlineData(3, "moderada")]
    [InlineData(5, "alta")]
    [InlineData(6, "muito alta")]
    public void Confidence_follows_convergent_evidence_thresholds(int count, string expected)
        => Assert.Equal(expected, EvidenceConfidence.Classify(count));

    [Fact]
    public void Fewer_than_three_evidences_never_produce_a_conclusion()
    {
        var result = new ValoraInsightDevolutivaService().Analyze(
            "Processo observado", ["survey", "metric"], "Os sinais variam juntos",
            "Uma causa", "Um impacto", "alta", ["Executar ação"]);

        Assert.False(result.EvidenciasSuficientes);
        Assert.Equal("baixa", result.Confianca);
        Assert.Null(result.CausaProvavel);
        Assert.Equal(ValoraInsightDevolutivaService.InsufficientEvidenceMessage, result.LimitesDaAnalise);
    }

    [Fact]
    public void Three_distinct_evidences_allow_an_explicitly_limited_hypothesis()
    {
        var result = new ValoraInsightDevolutivaService().Analyze(
            "Processo observado", ["survey", "metric", "document"], "Os sinais variam juntos",
            "Hipótese a validar", "Impacto provável", "moderada", ["Validar no próximo ciclo"]);

        Assert.True(result.EvidenciasSuficientes);
        Assert.Equal("moderada", result.Confianca);
        Assert.Contains("não comprova causalidade", result.LimitesDaAnalise);
    }
}
