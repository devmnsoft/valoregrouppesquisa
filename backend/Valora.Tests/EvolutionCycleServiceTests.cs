using System.Text.Json;
using Valora.Application.Evolution;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Tests;

public sealed class EvolutionCycleServiceTests {
    private static readonly Guid OrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SurveyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task OpenWithOrganizationalMeasurementUsesValidatedReadingInsteadOfPostedBaseline() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]));

        var id = await service.Open(OrganizationId, UserId, Request(SurveyId) with { BaselineScore = 1m }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.NotNull(repository.LastOpen);
        Assert.Equal(72.5m, repository.LastOpen!.BaselineScore);
        Assert.Equal("organizational_measurement", repository.LastOpen.BaselineSource);
        Assert.Equal("professional@3", repository.LastOpen.MethodologyVersion);
        Assert.Equal("population-a", repository.LastOpen.PopulationSignature);
        Assert.Equal("people", repository.LastOpen.DimensionSignature);
        Assert.Equal(SurveyId, repository.LastOpen.DiagnosticId);
        Assert.Equal(SurveyId, repository.LastOpen.ResultId);
        Assert.Contains("Medição inicial selecionada", repository.LastOpen.EvidenceSummary);
        Assert.Contains("4 resposta(s) pontuada(s)", repository.LastOpen.EvidenceSummary);
    }

    [Fact]
    public async Task OpenRejectsMeasurementThatIsNotAvailableForOrganization() {
        var repository = new CaptureEvolutionRepository();
        var service = new EvolutionCycleService(repository, new IntelligenceRepositoryStub([Reading()]));

        await Assert.ThrowsAsync<ArgumentException>(() => service.Open(OrganizationId, UserId, Request(Guid.NewGuid()), CancellationToken.None));

        Assert.Null(repository.LastOpen);
    }

    private static OpenEvolutionCycleRequest Request(Guid resultId) => new(
        DiagnosticId: null,
        ResultId: resultId,
        GovernanceCycleId: null,
        Title: "Ciclo validado",
        Summary: "Acompanhar evolução sem contaminar a medição.",
        BaselineScore: null,
        TargetScore: 85m,
        BaselineLevel: null,
        PeriodStart: new DateTime(2026, 1, 1),
        PeriodEnd: null,
        EvidenceSummary: "Evidência inicial registrada.");

    private static EvolutionSurveyComparisonDto Reading() => new(
        SurveyId,
        "Diagnóstico de Cultura",
        new DateTime(2026, 1, 1),
        new DateTime(2026, 2, 1),
        """
        {"methodology":{"code":"professional","version":3},"questions":[{"dimension":"people","scale":{"min":1,"max":5}}]}
        """,
        4,
        "population-a",
        72.5m,
        ["people"],
        [new EvolutionQuestionCriterionRow("people", "trust", "maturity", "1", 1)]);

    private sealed class CaptureEvolutionRepository : IEvolutionRepository {
        public OpenEvolutionCycleRequest? LastOpen { get; private set; }
        public Task<IReadOnlyList<EvolutionCycleDto>> List(Guid o, CancellationToken c) => Task.FromResult<IReadOnlyList<EvolutionCycleDto>>(Array.Empty<EvolutionCycleDto>());
        public Task<EvolutionCycleDto?> Get(Guid o, Guid id, CancellationToken c) => Task.FromResult<EvolutionCycleDto?>(null);
        public Task<IReadOnlyList<EvolutionSnapshotDto>> Snapshots(Guid o, Guid cycle, CancellationToken c) => Task.FromResult<IReadOnlyList<EvolutionSnapshotDto>>(Array.Empty<EvolutionSnapshotDto>());
        public Task<Guid> Open(Guid o, Guid u, OpenEvolutionCycleRequest r, CancellationToken c) { LastOpen = r; return Task.FromResult(Guid.NewGuid()); }
        public Task<Guid> Snapshot(Guid o, Guid u, Guid cycle, string evidence, string interpretation, string recommendation, CancellationToken c) => Task.FromResult(Guid.NewGuid());
        public Task Close(Guid o, Guid u, Guid cycle, string learning, CancellationToken c) => Task.CompletedTask;
    }

    private sealed class IntelligenceRepositoryStub(IReadOnlyList<EvolutionSurveyComparisonDto> readings) : IOrganizationalIntelligenceRepository {
        public Task<IReadOnlyList<EvolutionSurveyComparisonDto>> ListEvolutionComparisonSurveysAsync(Guid organizationId, CancellationToken ct) => Task.FromResult(readings);
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
    }
}
