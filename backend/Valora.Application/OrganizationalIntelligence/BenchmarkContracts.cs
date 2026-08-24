using System.Text.Json;

namespace Valora.Application.OrganizationalIntelligence;

public static class BenchmarkLimits
{
    public const string ExternalUnavailable = "Benchmark externo indisponível por amostra insuficiente.";
}

public sealed record BenchmarkSettings(int MinimumOrganizations = 5, int MinimumResponses = 50,
    bool ExternalEnabled = false, bool RequireAnonymization = true);
public sealed record BenchmarkDimension(string Code, string Name, decimal Score, decimal? ReferenceScore,
    decimal? Delta, string Trend, int EvidenceCount);
public sealed record BenchmarkSnapshotDto(Guid Id, Guid OrganizationId, Guid? SurveyId, Guid? ResultId,
    string SnapshotType, decimal MaturityScore, string MaturityLevel, int TotalResponses,
    IReadOnlyList<BenchmarkDimension> Dimensions, string EvidenceSummary, DateTime GeneratedAt,
    JsonElement Metadata);
public sealed record BenchmarkComparisonDto(Guid Id, Guid OrganizationId, Guid BaseSnapshotId,
    Guid? ComparedSnapshotId, string ComparisonType, decimal? ScoreDelta, string MaturityDelta,
    IReadOnlyList<string> Strengths, IReadOnlyList<string> Risks, IReadOnlyList<string> Opportunities,
    IReadOnlyList<string> Recommendations, string Limitation, DateTime CreatedAt);
public sealed record BenchmarkDashboardDto(IReadOnlyList<BenchmarkSnapshotDto> Snapshots,
    BenchmarkComparisonDto? LatestComparison, BenchmarkSettings Settings, bool ExternalAvailable,
    string? ExternalLimitation);
public sealed record GenerateBenchmarkRequest(Guid SurveyId, Guid? ResultId = null, string SnapshotType = "internal");
public sealed record CompareBenchmarkRequest(Guid BaseSnapshotId, Guid? ComparedSnapshotId = null,
    string ComparisonType = "historical");

public interface IBenchmarkRepository
{
    Task<BenchmarkSettings> SettingsAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<BenchmarkSnapshotDto>> ListAsync(Guid organizationId, CancellationToken ct);
    Task<BenchmarkSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<BenchmarkSnapshotDto> GenerateAsync(Guid organizationId, GenerateBenchmarkRequest request, CancellationToken ct);
    Task<BenchmarkComparisonDto> CompareAsync(Guid organizationId, CompareBenchmarkRequest request, CancellationToken ct);
    Task SaveSettingsAsync(Guid organizationId, BenchmarkSettings settings, CancellationToken ct);
}

public interface IBenchmarkManagementService
{
    Task<BenchmarkDashboardDto> DashboardAsync(Guid organizationId, CancellationToken ct);
    Task<BenchmarkSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<BenchmarkSnapshotDto> GenerateAsync(Guid organizationId, GenerateBenchmarkRequest request, CancellationToken ct);
    Task<BenchmarkComparisonDto> CompareAsync(Guid organizationId, CompareBenchmarkRequest request, CancellationToken ct);
    Task<BenchmarkSettings> UpdateSettingsAsync(Guid organizationId, BenchmarkSettings settings, CancellationToken ct);
}
