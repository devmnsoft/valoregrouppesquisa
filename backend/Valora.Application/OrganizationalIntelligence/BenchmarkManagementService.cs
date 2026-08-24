namespace Valora.Application.OrganizationalIntelligence;

public sealed class BenchmarkManagementService(IBenchmarkRepository repository) : IBenchmarkManagementService
{
    public async Task<BenchmarkDashboardDto> DashboardAsync(Guid organizationId, CancellationToken ct)
    {
        var settings = await repository.SettingsAsync(organizationId, ct);
        var snapshots = await repository.ListAsync(organizationId, ct);
        var external = settings.ExternalEnabled && snapshots.Any(x => x.SnapshotType is "sector" or "global" &&
            x.TotalResponses >= settings.MinimumResponses);
        return new(snapshots, null, settings, external, external ? null : BenchmarkLimits.ExternalUnavailable);
    }

    public Task<BenchmarkSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct) => repository.GetAsync(organizationId, id, ct);
    public Task<BenchmarkSnapshotDto> GenerateAsync(Guid organizationId, GenerateBenchmarkRequest request, CancellationToken ct)
    {
        if (request.SurveyId == Guid.Empty) throw new ArgumentException("Selecione um diagnóstico real.");
        if (request.SnapshotType is not ("internal" or "historical" or "unit" or "plan"))
            throw new InvalidOperationException(BenchmarkLimits.ExternalUnavailable);
        return repository.GenerateAsync(organizationId, request, ct);
    }
    public Task<BenchmarkComparisonDto> CompareAsync(Guid organizationId, CompareBenchmarkRequest request, CancellationToken ct) =>
        repository.CompareAsync(organizationId, request, ct);
    public async Task<BenchmarkSettings> UpdateSettingsAsync(Guid organizationId, BenchmarkSettings settings, CancellationToken ct)
    {
        if (settings.MinimumOrganizations < 2 || settings.MinimumResponses < 5)
            throw new ArgumentException("A amostra mínima deve preservar anonimização e significância.");
        await repository.SaveSettingsAsync(organizationId, settings, ct); return settings;
    }
}
