using System.Text.Encodings.Web;
using System.Text.Json;
using Valora.Application.OrganizationalIntelligence;
using Xunit;

namespace Valora.Tests;

// B1 - Integração da EvolutionComparisonService ao fluxo real: leitura por survey
// com snapshot metodológico imutável, seleção servidora e veredito sem tendência.
public sealed class EvolutionComparisonQueryTests {
    private static readonly Guid Org = Guid.Parse("7f9c1d2e-0000-4000-8000-0000000000aa");
    private static readonly Guid First = Guid.Parse("ccccccc1-0000-4000-8000-000000000001");
    private static readonly Guid Second = Guid.Parse("ccccccc2-0000-4000-8000-000000000002");
    private static readonly Guid Other = Guid.Parse("ccccccc9-0000-4000-8000-000000000009");
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static string Snapshot(int version = 1, int scaleMax = 5) =>
        """{"methodology":{"code":"valora-diagnosis","version":"__VERSION__"},"questions":[{"dimension":"people","scale":{"min":0,"max":"__MAX__"}}]}"""
            .Replace("\"__VERSION__\"", version.ToString())
            .Replace("\"__MAX__\"", scaleMax.ToString());

    private static string TwoDimensionSnapshot() =>
        """{"methodology":{"code":"valora-diagnosis","version":1},"questions":[{"dimension":"people","scale":{"min":0,"max":5}},{"dimension":"process","scale":{"min":0,"max":5}}]}""";

    private static EvolutionQuestionCriterionRow Criterion(string dimension) => new(dimension, "metric", "idx", "1.0000", 1);

    private static EvolutionSurveyComparisonDto Survey(Guid id, decimal? maturity, int scored, string snapshot, string population,
        IReadOnlyList<string> dimensions, params EvolutionQuestionCriterionRow[] criteria) =>
        new(id, id == First ? "Leitura inicial" : "Leitura atual", T0, T0.AddHours(1), snapshot, scored, population, maturity,
            dimensions.ToList(), criteria.ToList());

    private static EvolutionSurveyComparisonDto FirstSurvey() =>
        Survey(First, 68m, 3, Snapshot(), "population-a", ["people"], Criterion("people"));

    private static EvolutionSurveyComparisonDto SecondSurvey() =>
        Survey(Second, 74.5m, 3, Snapshot(), "population-a", ["people"], Criterion("people"));

    private OrganizationalIntelligenceService Service(params EvolutionSurveyComparisonDto[] surveys) =>
        new(new ComparisonRepository(surveys.ToList()));

    [Fact]
    public async Task Candidates_expose_only_comparison_facts_in_persisted_order() {
        var candidates = await Service(FirstSurvey(), SecondSurvey()).EvolutionComparisonCandidatesAsync(Org, default);

        Assert.Equal(2, candidates.Count);
        Assert.Equal(First, candidates[0].SurveyId);
        Assert.Equal(Second, candidates[1].SurveyId);
        Assert.Equal("Leitura inicial", candidates[0].Title);
        Assert.Equal(T0.AddHours(1), candidates[0].CapturedAt);
        Assert.Equal(68m, candidates[0].MaturityIndex);
        Assert.Equal(3, candidates[0].ScoredResponseCount);
    }

    [Fact]
    public async Task Default_selection_uses_oldest_baseline_and_newest_current() {
        var result = await Service(FirstSurvey(), SecondSurvey()).EvolutionComparisonAsync(Org, null, null, default);

        Assert.NotNull(result);
        Assert.Equal(EvolutionComparisonSchemas.V1, result!.SchemaVersion);
        Assert.Equal(First, result.Baseline.SurveyId);
        Assert.Equal(Second, result.Current.SurveyId);
        Assert.True(result.Baseline.DataAvailable);
        Assert.True(result.Current.DataAvailable);
        Assert.Equal("valora-diagnosis@1", result.Baseline.MethodologyVersion);
        Assert.Equal("valora-diagnosis@1", result.Current.MethodologyVersion);
        Assert.True(result.Comparison.Comparable);
        Assert.Equal(6.5m, result.Comparison.AbsoluteVariation);
        Assert.Equal("pontos da escala 0 a 100", result.Comparison.Unit);
        Assert.Contains("não demonstra causalidade", result.Comparison.Limitation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Explicit_selection_is_respected_and_variation_keeps_its_sign() {
        var result = await Service(FirstSurvey(), SecondSurvey()).EvolutionComparisonAsync(Org, Second, First, default);

        Assert.NotNull(result);
        Assert.Equal(Second, result!.Baseline.SurveyId);
        Assert.Equal(First, result.Current.SurveyId);
        Assert.True(result.Comparison.Comparable);
        Assert.Equal(-6.5m, result.Comparison.AbsoluteVariation);
    }

    [Fact]
    public async Task Explicit_id_outside_the_eligible_set_returns_null() {
        var service = Service(FirstSurvey(), SecondSurvey());

        Assert.Null(await service.EvolutionComparisonAsync(Org, Other, Second, default));
        Assert.Null(await service.EvolutionComparisonAsync(Org, First, Other, default));
    }

    [Fact]
    public async Task Organization_without_eligible_surveys_returns_empty_options_and_null_comparison() {
        var service = Service();

        Assert.Empty(await service.EvolutionComparisonCandidatesAsync(Org, default));
        Assert.Null(await service.EvolutionComparisonAsync(Org, null, null, default));
    }

    [Fact]
    public async Task Single_eligible_survey_compares_against_itself_and_reports_the_limitation() {
        var result = await Service(FirstSurvey()).EvolutionComparisonAsync(Org, null, null, default);

        Assert.NotNull(result);
        Assert.False(result!.Comparison.Comparable);
        Assert.Null(result.Comparison.AbsoluteVariation);
        Assert.Contains("mesma avaliação", result.Comparison.Limitation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Same_survey_on_both_sides_is_not_comparable() {
        var result = await Service(FirstSurvey(), SecondSurvey()).EvolutionComparisonAsync(Org, First, First, default);

        Assert.NotNull(result);
        Assert.False(result!.Comparison.Comparable);
        Assert.Contains("mesma avaliação", result.Comparison.Limitation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Different_methodology_versions_are_not_comparable() {
        var other = Survey(Second, 74.5m, 3, Snapshot(version: 2), "population-a", ["people"], Criterion("people"));
        var changed = await Service(FirstSurvey(), other).EvolutionComparisonAsync(Org, null, null, default);

        Assert.NotNull(changed);
        Assert.False(changed!.Comparison.Comparable);
        Assert.Null(changed.Comparison.AbsoluteVariation);
        Assert.Contains("versão metodológica", changed.Comparison.Limitation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Different_measured_dimensions_are_not_comparable() {
        var other = Survey(Second, 74.5m, 3, TwoDimensionSnapshot(), "population-a",
            ["people", "process"], Criterion("people"), Criterion("process"));
        var changed = await Service(FirstSurvey(), other).EvolutionComparisonAsync(Org, null, null, default);

        Assert.NotNull(changed);
        Assert.False(changed!.Comparison.Comparable);
        Assert.Contains("dimensões", changed.Comparison.Limitation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Different_scales_are_not_comparable() {
        var other = Survey(Second, 74.5m, 3, Snapshot(scaleMax: 10), "population-a", ["people"], Criterion("people"));
        var changed = await Service(FirstSurvey(), other).EvolutionComparisonAsync(Org, null, null, default);

        Assert.NotNull(changed);
        Assert.False(changed!.Comparison.Comparable);
        Assert.Contains("escala", changed.Comparison.Limitation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Different_populations_are_not_comparable() {
        var other = Survey(Second, 74.5m, 3, Snapshot(), "population-b", ["people"], Criterion("people"));
        var changed = await Service(FirstSurvey(), other).EvolutionComparisonAsync(Org, null, null, default);

        Assert.NotNull(changed);
        Assert.False(changed!.Comparison.Comparable);
        Assert.Contains("população ou recorte", changed.Comparison.Limitation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_score_on_one_side_reports_unavailable_results() {
        var other = Survey(Second, null, 0, Snapshot(), "population-a", Array.Empty<string>());
        var changed = await Service(FirstSurvey(), other).EvolutionComparisonAsync(Org, null, null, default);

        Assert.NotNull(changed);
        Assert.False(changed!.Current.DataAvailable);
        Assert.Null(changed.Comparison.AbsoluteVariation);
        Assert.Equal("Os resultados não estão disponíveis para comparação.", changed.Comparison.Limitation);
    }

    [Fact]
    public async Task Response_serialization_is_stable_and_versioned() {
        var json = JsonSerializer.Serialize(await CreateComparableResult(), new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            // O contrato testado aqui é forma, ordem e versão; o wire aplica o
            // encoder padrão do framework sobre o mesmo objeto serializado.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        const string expected = """{"schemaVersion":"evolution-comparison/v1","baseline":{"surveyId":"ccccccc1-0000-4000-8000-000000000001","title":"Leitura inicial","capturedAt":"2026-10-01T13:00:00Z","maturityIndex":68,"scoredResponseCount":3,"methodologyVersion":"valora-diagnosis@1","dataAvailable":true},"current":{"surveyId":"ccccccc2-0000-4000-8000-000000000002","title":"Leitura atual","capturedAt":"2026-10-01T13:00:00Z","maturityIndex":74.5,"scoredResponseCount":3,"methodologyVersion":"valora-diagnosis@1","dataAvailable":true},"comparison":{"comparable":true,"absoluteVariation":6.5,"unit":"pontos da escala 0 a 100","limitation":"Variação absoluta observada; não demonstra causalidade do plano."}}""";
        Assert.Equal(expected, json);
    }

    private async System.Threading.Tasks.Task<EvolutionComparisonDto> CreateComparableResult() =>
        await Service(FirstSurvey(), SecondSurvey()).EvolutionComparisonAsync(Org, null, null, default)!;

    private sealed class ComparisonRepository(IReadOnlyList<EvolutionSurveyComparisonDto> surveys) : IOrganizationalIntelligenceRepository {
        public Task<EvidenceSummaryDto> GetEvidenceAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<OrganizationalIntelligenceDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> ListRunsAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<OrganizationalIntelligenceRunDto?> GetRunAsync(Guid organizationId, Guid id, CancellationToken ct) => throw new NotImplementedException();
        public Task SaveAnalysisAsync(OrganizationalIntelligenceRunDto run, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<OrganizationalJourneyEventDto>> ListJourneyAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<OrganizationalJourneyEventDto> CreateJourneyEventAsync(OrganizationalJourneyEventDto item, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<ValoraIndicatorDefinitionDto>> ListIndicatorsAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<ValoraActionDto>> ListActionsAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<ValoraActionDto> CreateActionAsync(ValoraActionDto item, Guid userId, CancellationToken ct) => throw new NotImplementedException();
        public Task<ValoraActionDto?> UpdateActionAsync(Guid organizationId, Guid actionId, UpdateValoraActionRequest request, Guid userId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<ValoraActionHistoryDto>> ListActionHistoryAsync(Guid organizationId, Guid actionId, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> DeleteActionAsync(Guid organizationId, Guid actionId, Guid userId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<EvidenceItemDto>> ListEvidenceItemsAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<IntelligenceModuleRecordDto>> ListModuleRecordsAsync(Guid organizationId, string module, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<EvolutionSurveyComparisonDto>> ListEvolutionComparisonSurveysAsync(Guid organizationId, CancellationToken ct) => Task.FromResult(surveys);
    }
}
