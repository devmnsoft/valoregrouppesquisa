using Valora.Application.Methodology;
using Valora.Application.OrganizationalIntelligence;
using Valora.Application.Results;
using Valora.Application.ValoraAi;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "OfficialMaturityEngine")]
public sealed class MaturityEngineOfficialValidationTests {

    [Fact]
    public void ConfidenceCalculator_RejectsModerateConfidence_WhenEvidenceCountIsLessThan3() {
        // Regra Oficial Sprint R2: Menos de 3 evidências NUNCA pode receber confiança Moderada ou Alta
        var confidence1 = ValoraConfidenceCalculator.Calculate(evidenceCount: 1, convergentSources: 1);
        var confidence2 = ValoraConfidenceCalculator.Calculate(evidenceCount: 2, convergentSources: 2);
        var confidence0 = ValoraConfidenceCalculator.Calculate(evidenceCount: 0, convergentSources: 0);

        Assert.Equal(AiConfidence.Low, confidence1);
        Assert.Equal(AiConfidence.Low, confidence2);
        Assert.Equal(AiConfidence.Insufficient, confidence0);
    }

    [Fact]
    public void ConfidenceCalculator_RejectsModerateOrHighConfidence_WhenSourcesAreNotConvergent() {
        // Regra Oficial Sprint R2: Mesmo com 4 ou 5 evidências, se todas vierem de fonte única (fontes < 2), a confiança é Baixa
        var confidenceSingleSource = ValoraConfidenceCalculator.Calculate(evidenceCount: 4, convergentSources: 1);
        var confidenceSingleSource5 = ValoraConfidenceCalculator.Calculate(evidenceCount: 5, convergentSources: 1);

        Assert.Equal(AiConfidence.Low, confidenceSingleSource);
        Assert.Equal(AiConfidence.Low, confidenceSingleSource5);
    }

    [Fact]
    public void ConfidenceCalculator_GrantsMediumOrHigh_OnlyWithAtLeast3Evidence_AndAtLeast2ConvergentSources() {
        var confidenceMedium = ValoraConfidenceCalculator.Calculate(evidenceCount: 3, convergentSources: 2);
        var confidenceHigh = ValoraConfidenceCalculator.Calculate(evidenceCount: 5, convergentSources: 3);

        Assert.Equal(AiConfidence.Medium, confidenceMedium);
        Assert.Equal(AiConfidence.High, confidenceHigh);
    }

    [Fact]
    public void StrategicMaturityScoring_ReproducesExactScores_AndDeterministicLevelIntervals() {
        var service = new StrategicMaturityScoringService();

        // 25 perguntas com valor 4 (total 100) -> Consistência (86-110)
        var answersConsistency = Enumerable.Range(1, 25).Select(i => {
            var dim = i switch {
                <= 5 => "culture_purpose",
                <= 10 => "management_governance",
                <= 15 => "leadership",
                <= 20 => "people_talents",
                _ => "results_growth"
            };
            return new StrategicMaturityAnswer($"VALORA_MATURITY_Q{i:00}", dim, 4);
        }).ToList();

        var resultConsistency = service.Calculate(answersConsistency);

        Assert.Equal(100, resultConsistency.MainTotal);
        Assert.Equal("consistency", resultConsistency.Level);
        Assert.Equal("🟢 Consistência", resultConsistency.LevelLabel);
        Assert.Equal(4.0m, resultConsistency.MainAverage);
        Assert.Equal(5, resultConsistency.Dimensions.Count);
        Assert.All(resultConsistency.Dimensions, d => Assert.Equal(20, d.Score));
    }

    [Fact]
    public void StrategicMaturityScoring_RejectsInvalidValues_OrMissingQuestions() {
        var service = new StrategicMaturityScoringService();

        var invalidAnswers = Enumerable.Range(1, 25).Select(i =>
            new StrategicMaturityAnswer($"VALORA_MATURITY_Q{i:00}", "leadership", i == 1 ? 6 : 3)
        ).ToList();

        Assert.Throws<ArgumentException>(() => service.Calculate(invalidAnswers));

        var incompleteAnswers = Enumerable.Range(1, 20).Select(i =>
            new StrategicMaturityAnswer($"VALORA_MATURITY_Q{i:00}", "leadership", 3)
        ).ToList();

        Assert.Throws<InvalidOperationException>(() => service.Calculate(incompleteAnswers));
    }

    [Fact]
    public void ValoraIntelligenceEngine_NeverTurnsMissingEvidence_IntoZeroScore() {
        var engine = new ValoraIntelligenceEngine();
        var orgId = Guid.NewGuid();

        // Amostra sem nenhuma evidência
        var resultEmpty = engine.Analyze(orgId, null, []);

        Assert.Null(resultEmpty.Score);
        Assert.Equal("Não avaliado", resultEmpty.MaturityLevel);
        Assert.Equal("low", resultEmpty.ConfidenceLevel);
        Assert.Contains("Não há evidências válidas para produzir diagnóstico", resultEmpty.Interpretation);
        Assert.Empty(resultEmpty.Dimensions);
    }

    [Fact]
    public void ValoraIntelligenceEngine_CalculatesDeterministicWeightedScores() {
        var engine = new ValoraIntelligenceEngine();
        var orgId = Guid.NewGuid();
        var surveyId = Guid.NewGuid();

        var evidence = new List<DiagnosisEvidence> {
            new(Guid.NewGuid(), orgId, surveyId, "Liderança", "lid_com", 80m, 1.0m, "Comunicação clara", DateTime.UtcNow),
            new(Guid.NewGuid(), orgId, surveyId, "Liderança", "lid_aut", 100m, 1.0m, "Autonomia operacional", DateTime.UtcNow),
            new(Guid.NewGuid(), orgId, surveyId, "Processos", "prc_pad", 60m, 2.0m, "Padronização de rotinas", DateTime.UtcNow),
            new(Guid.NewGuid(), orgId, surveyId, "Processos", "prc_sla", 80m, 1.0m, "SLAs de entrega", DateTime.UtcNow)
        };

        var result = engine.Analyze(orgId, surveyId, evidence);

        Assert.NotNull(result.Score);
        Assert.Equal(2, result.Dimensions.Count);

        var lid = result.Dimensions.Single(d => d.Dimension == "Liderança");
        Assert.Equal(90.0m, lid.Score); // (80*1 + 100*1)/2 = 90

        var prc = result.Dimensions.Single(d => d.Dimension == "Processos");
        Assert.Equal(66.67m, prc.Score); // (60*2 + 80*1)/3 = 66.67
    }
}
