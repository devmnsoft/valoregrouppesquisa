namespace Valora.Application.Heatmap;

public sealed record HeatmapFilter(Guid DiagnosticId, Guid? ResultId = null, string ViewBy = "dimension",
    string? Area = null, string? Unit = null, string? Leadership = null, string? IndexCode = null,
    DateTime? PeriodStart = null, DateTime? PeriodEnd = null);
public sealed record HeatmapCellDto(Guid Id, string Dimension, string? IndexCode, string? AreaName,
    string? UnitName, string? LeadershipName, decimal? Score, string Level, string RiskLevel,
    string Trend, int ResponseCount, string EvidenceSummary, string Recommendation, bool InsufficientSample);
public sealed record HeatmapSnapshotDto(Guid Id, Guid OrganizationId, Guid DiagnosticId, Guid? ResultId,
    string Title, string SnapshotType, string Status, DateTime GeneratedAt, string EvidenceSummary,
    string? AiSummary, IReadOnlyList<HeatmapCellDto> Cells);
public sealed record GenerateHeatmapRequest(Guid DiagnosticId, Guid? ResultId = null, string ViewBy = "dimension",
    string? Area = null, string? Unit = null, string? Leadership = null, string? IndexCode = null,
    DateTime? PeriodStart = null, DateTime? PeriodEnd = null);

public interface IHeatmapRepository
{
    Task<IReadOnlyList<HeatmapSnapshotDto>> ListAsync(Guid organizationId, CancellationToken ct);
    Task<HeatmapSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<HeatmapSnapshotDto> GenerateAsync(Guid organizationId, Guid? userId, HeatmapFilter filter, CancellationToken ct);
}

public interface IHeatmapService
{
    Task<IReadOnlyList<HeatmapSnapshotDto>> OverviewAsync(Guid organizationId, CancellationToken ct);
    Task<HeatmapSnapshotDto?> GetAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<HeatmapSnapshotDto> GenerateAsync(Guid organizationId, Guid? userId, GenerateHeatmapRequest request, CancellationToken ct);
}
