using Valora.Application.OrganizationalIntelligence;

namespace Valora.Tests;

public sealed class BenchmarkManagementServiceTests
{
    [Fact]
    public async Task Dashboard_never_invents_external_reference()
    {
        var service = new BenchmarkManagementService(new FakeRepository());
        var dashboard = await service.DashboardAsync(Guid.NewGuid(), default);
        Assert.False(dashboard.ExternalAvailable);
        Assert.Equal(BenchmarkLimits.ExternalUnavailable, dashboard.ExternalLimitation);
    }

    [Fact]
    public async Task Generate_rejects_external_snapshot_without_reference_group()
    {
        var service = new BenchmarkManagementService(new FakeRepository());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(Guid.NewGuid(), new(Guid.NewGuid(), SnapshotType: "sector"), default));
        Assert.Equal(BenchmarkLimits.ExternalUnavailable, error.Message);
    }

    private sealed class FakeRepository : IBenchmarkRepository
    {
        public Task<BenchmarkSettings> SettingsAsync(Guid organizationId, CancellationToken ct) => Task.FromResult(new BenchmarkSettings());
        public Task<IReadOnlyList<BenchmarkSnapshotDto>> ListAsync(Guid organizationId, CancellationToken ct) => Task.FromResult<IReadOnlyList<BenchmarkSnapshotDto>>([]);
        public Task<BenchmarkSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct) => Task.FromResult<BenchmarkSnapshotDto?>(null);
        public Task<BenchmarkSnapshotDto> GenerateAsync(Guid organizationId, GenerateBenchmarkRequest request, CancellationToken ct) => throw new NotImplementedException();
        public Task<BenchmarkComparisonDto> CompareAsync(Guid organizationId, CompareBenchmarkRequest request, CancellationToken ct) => throw new NotImplementedException();
        public Task SaveSettingsAsync(Guid organizationId, BenchmarkSettings settings, CancellationToken ct) => Task.CompletedTask;
    }
}
