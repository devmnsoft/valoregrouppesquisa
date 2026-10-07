using System.Text.Json;
using Valora.Application.OrganizationalIntelligence;
using Xunit;

namespace Valora.Tests;

public sealed class EvolutionSelectionTests {
    private static readonly Guid Org = Guid.Parse("7f9c1d2e-0000-4000-8000-000000000001");
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Repeated_measurement_of_the_same_population_is_not_a_new_temporal_point() {
        var repository = new ScenarioRepository(new[] {
            Run(Id(1), T0, 68m, 50),
            Run(Id(2), T0.AddHours(1), 68m, 50)
        });
        var points = await new OrganizationalIntelligenceService(repository).EvolutionAsync(Org, default);

        Assert.Single(points);
        Assert.Equal("baseline", points[0].Classification);
        Assert.Equal(68m, points[0].MaturityIndex);
        Assert.Equal(0m, points[0].Change);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Population_shift_between_runs_is_not_classified_as_evolution() {
        var repository = new ScenarioRepository(new[] {
            Run(Id(1), T0, 68m, 50),
            Run(Id(2), T0.AddHours(1), 71m, 60)
        });
        var points = await new OrganizationalIntelligenceService(repository).EvolutionAsync(Org, default);

        Assert.Equal(2, points.Count);
        Assert.Equal("baseline", points[0].Classification);
        var second = points[1];
        Assert.Equal("not_comparable", second.Classification);
        Assert.Equal(3m, second.Change);
        Assert.NotNull(second.Limitation);
        Assert.Contains("50", second.Limitation, StringComparison.Ordinal);
        Assert.Contains("60", second.Limitation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Same_population_deltas_keep_the_existing_classification_thresholds() {
        var repository = new ScenarioRepository(new[] {
            Run(Id(1), T0, 68m, 50),
            Run(Id(2), T0.AddMinutes(1), 70.5m, 50),
            Run(Id(3), T0.AddMinutes(2), 68m, 50),
            Run(Id(4), T0.AddMinutes(3), 68.3m, 50),
            Run(Id(5), T0.AddMinutes(4), 69m, 50)
        });
        var points = await new OrganizationalIntelligenceService(repository).EvolutionAsync(Org, default);

        Assert.Equal(5, points.Count);
        Assert.Equal(new[] { "baseline", "evolution", "regression", "stagnation", "stable" }, points.Select(x => x.Classification).ToArray());
        Assert.Equal(new[] { 0m, 2.5m, -2.5m, 0.3m, 0.7m }, points.Select(x => x.Change).ToArray());
        Assert.All(points, x => Assert.False(x.HasSufficientHistory));
        Assert.All(points, x => Assert.Null(x.EstimatedNextCycle));
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Equal_timestamps_resolve_by_id_and_do_not_depend_on_repository_order() {
        var earlyId = Guid.Parse("00000000-0000-4000-8000-000000000002");
        var lateId = Guid.Parse("ffffffff-0000-4000-8000-000000000001");
        var first = Run(lateId, T0, 70m, 51);
        var second = Run(earlyId, T0, 68m, 50);

        var ordered = await new OrganizationalIntelligenceService(new ScenarioRepository(new[] { first, second })).EvolutionAsync(Org, default);
        var reversed = await new OrganizationalIntelligenceService(new ScenarioRepository(new[] { second, first })).EvolutionAsync(Org, default);

        Assert.Equal(68m, ordered[0].MaturityIndex);
        Assert.Equal("baseline", ordered[0].Classification);
        Assert.Equal("not_comparable", ordered[1].Classification);
        Assert.Equal(JsonSerializer.Serialize(ordered), JsonSerializer.Serialize(reversed));
    }

    [Fact]
    public async Task Audit_runs_without_maturity_are_skipped_without_shifting_the_series() {
        var repository = new ScenarioRepository(new[] {
            Run(Id(1), T0, null, 40),
            Run(Id(2), T0.AddHours(1), 66m, 55),
            Run(Id(3), T0.AddHours(2), 69.5m, 55)
        });
        var points = await new OrganizationalIntelligenceService(repository).EvolutionAsync(Org, default);

        Assert.Equal(2, points.Count);
        Assert.Equal(66m, points[0].MaturityIndex);
        Assert.Equal("baseline", points[0].Classification);
        Assert.Equal("evolution", points[1].Classification);
    }

    private static Guid Id(int n) => Guid.Parse($"bbbbbbb{n}-0000-4000-8000-00000000000{n}");

    private static OrganizationalIntelligenceRunDto Run(Guid id, DateTime createdAt, decimal? maturity, int evidenceCount) =>
        new(id, Org, maturity, maturity, maturity, 0m, "sem dimensão", "sem dimensão", evidenceCount, "moderate", null, new List<DimensionHeatmapDto>(), Array.Empty<OrganizationalInsightDto>(), createdAt);

    private sealed class ScenarioRepository(IReadOnlyList<OrganizationalIntelligenceRunDto> runs) : IOrganizationalIntelligenceRepository {
        public int SaveCalls { get; private set; }
        public Task<EvidenceSummaryDto> GetEvidenceAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<OrganizationalIntelligenceDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> ListRunsAsync(Guid organizationId, CancellationToken ct) => Task.FromResult(runs);
        public Task<OrganizationalIntelligenceRunDto?> GetRunAsync(Guid organizationId, Guid id, CancellationToken ct) => throw new NotImplementedException();
        public Task SaveAnalysisAsync(OrganizationalIntelligenceRunDto run, CancellationToken ct) { SaveCalls++; return Task.CompletedTask; }
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
        public Task<IReadOnlyList<EvolutionSurveyComparisonDto>> ListEvolutionComparisonSurveysAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
    }
}
