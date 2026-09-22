using Valora.Application.OrganizationalIntelligence;

namespace Valora.Tests;

public sealed class OrganizationalIntelligenceSemanticsTests {
    [Fact]
    public async Task Missing_dimensions_are_persisted_as_not_evaluated() {
        var repository = new StubRepository(new(0, 0, 0, 0, []));
        var result = await new OrganizationalIntelligenceService(repository).GenerateAsync(Guid.NewGuid(), default);

        Assert.Null(result.MaturityIndex);
        Assert.Null(result.CultureTrustIndex);
        Assert.Null(result.GovernanceExecutionIndex);
        Assert.Equal("insufficient_evidence", result.ConfidenceLevel);
        Assert.Same(result, repository.Saved);
    }

    [Fact]
    public async Task A_measured_zero_is_not_replaced_by_general_maturity() {
        var dimensions = new[] {
            new DimensionHeatmapDto(Guid.NewGuid(), "cultura", "Cultura", 0, 3),
            new DimensionHeatmapDto(Guid.NewGuid(), "resultados", "Resultados", 80, 3)
        };
        var repository = new StubRepository(new(6, 0, 1, 0, dimensions));
        var result = await new OrganizationalIntelligenceService(repository).GenerateAsync(Guid.NewGuid(), default);

        Assert.Equal(40m, result.MaturityIndex);
        Assert.Equal(0m, result.CultureTrustIndex);
        Assert.Null(result.GovernanceExecutionIndex);
    }

    private sealed class StubRepository(EvidenceSummaryDto evidence) : IOrganizationalIntelligenceRepository {
        public OrganizationalIntelligenceRunDto? Saved { get; private set; }
        public Task<EvidenceSummaryDto> GetEvidenceAsync(Guid organizationId, CancellationToken ct) => Task.FromResult(evidence);
        public Task SaveAnalysisAsync(OrganizationalIntelligenceRunDto run, CancellationToken ct) { Saved = run; return Task.CompletedTask; }
        public Task<IReadOnlyList<OrganizationalIntelligenceRunDto>> ListRunsAsync(Guid organizationId, CancellationToken ct) => Task.FromResult<IReadOnlyList<OrganizationalIntelligenceRunDto>>([]);
        public Task<OrganizationalIntelligenceDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<OrganizationalIntelligenceRunDto?> GetRunAsync(Guid organizationId, Guid id, CancellationToken ct) => throw new NotImplementedException();
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
