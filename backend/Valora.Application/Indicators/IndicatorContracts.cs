using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Indicators;

public enum IndicatorTrend { Improving, Stable, Worsening, InsufficientData }

public sealed record IndicatorDto(Guid Id, string Name, string Category, string Unit, string? SourceName,
    Guid ResponsibleUserId, string Periodicity, string Status, bool IsCalculated, string? Formula,
    DateTime CreatedAt, int MeasurementCount, decimal? LatestValue, DateTime? LatestMeasuredAt)
{
    public bool HasReliableSource => !string.IsNullOrWhiteSpace(SourceName);
}
public sealed record IndicatorTargetDto(Guid Id, Guid IndicatorId, decimal TargetValue, DateTime PeriodStart,
    DateTime PeriodEnd, string ComparisonRule, Guid ResponsibleUserId, string Status);
public sealed record IndicatorMeasurementDto(Guid Id, Guid IndicatorId, decimal Value, DateTime MeasuredAt,
    string SourceName, Guid ResponsibleUserId, string? Justification, int Revision, Guid? DataHubImportId);
public sealed record IndicatorAlertDto(Guid Id, Guid IndicatorId, string IndicatorName, string Severity,
    string Message, string Status, DateTime CreatedAt, DateTime? ResolvedAt);
public sealed record TrendResult(IndicatorTrend Trend, int SampleSize, decimal? Delta, string Limitation);
public sealed record IndicatorDashboardDto(IReadOnlyList<IndicatorDto> Indicators,
    IReadOnlyList<IndicatorTargetDto> Targets, IReadOnlyList<IndicatorAlertDto> Alerts,
    IReadOnlyDictionary<Guid, TrendResult> Trends);

public sealed class CreateIndicatorRequest
{
    [Required, StringLength(160)] public string Name { get; set; } = "";
    [Required, StringLength(80)] public string Category { get; set; } = "";
    [Required, StringLength(30)] public string Unit { get; set; } = "";
    [Required, StringLength(160)] public string SourceName { get; set; } = "";
    [Required] public Guid ResponsibleUserId { get; set; }
    [Required, RegularExpression("daily|weekly|monthly|quarterly|yearly")] public string Periodicity { get; set; } = "monthly";
    public bool IsCalculated { get; set; }
    [StringLength(2000)] public string? Formula { get; set; }
}
public sealed class CreateTargetRequest
{
    [Required] public decimal? TargetValue { get; set; }
    [Required] public DateTime? PeriodStart { get; set; }
    [Required] public DateTime? PeriodEnd { get; set; }
    [Required, RegularExpression("higher_is_better|lower_is_better|ideal_range|exact")] public string ComparisonRule { get; set; } = "higher_is_better";
    [Required] public Guid ResponsibleUserId { get; set; }
}
public sealed class CreateMeasurementRequest
{
    [Required] public decimal? Value { get; set; }
    [Required] public DateTime? MeasuredAt { get; set; }
    [Required, StringLength(160)] public string SourceName { get; set; } = "";
    [Required] public Guid ResponsibleUserId { get; set; }
    [Required, StringLength(1000)] public string Justification { get; set; } = "";
    public Guid? DataHubImportId { get; set; }
}
public sealed class CreateScorecardRequest
{
    [Required, StringLength(160)] public string Name { get; set; } = "";
    [Required, StringLength(500)] public string Objective { get; set; } = "";
    [Required] public DateTime? PeriodStart { get; set; }
    [Required] public DateTime? PeriodEnd { get; set; }
    [MinLength(1)] public Guid[] IndicatorIds { get; set; } = [];
}
public sealed record ExecutiveScorecardDto(Guid Id, string Name, string Objective, DateTime PeriodStart, DateTime PeriodEnd, int ItemCount, DateTime CreatedAt);
public sealed record AnalyticsSnapshotDto(Guid Id, string Name, DateTime CapturedAt, string Limitation, int ItemCount);

public interface IIndicatorRepository
{
    Task<IReadOnlyList<IndicatorDto>> List(Guid organizationId, CancellationToken ct);
    Task<IndicatorDto?> Get(Guid organizationId, Guid id, CancellationToken ct);
    Task<Guid> Create(Guid organizationId, CreateIndicatorRequest request, CancellationToken ct);
    Task Archive(Guid organizationId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<IndicatorTargetDto>> Targets(Guid organizationId, Guid? indicatorId, CancellationToken ct);
    Task<Guid> CreateTarget(Guid organizationId, Guid indicatorId, CreateTargetRequest request, CancellationToken ct);
    Task<IReadOnlyList<IndicatorMeasurementDto>> Measurements(Guid organizationId, Guid indicatorId, CancellationToken ct);
    Task<Guid> CreateMeasurement(Guid organizationId, Guid indicatorId, CreateMeasurementRequest request, CancellationToken ct);
    Task<IReadOnlyList<IndicatorAlertDto>> Alerts(Guid organizationId, CancellationToken ct);
    Task ResolveAlert(Guid organizationId, Guid id, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<ExecutiveScorecardDto>> Scorecards(Guid organizationId, CancellationToken ct);
    Task<Guid> CreateScorecard(Guid organizationId, Guid userId, CreateScorecardRequest request, CancellationToken ct);
    Task<IReadOnlyList<AnalyticsSnapshotDto>> Snapshots(Guid organizationId, CancellationToken ct);
    Task<Guid> CreateSnapshot(Guid organizationId, Guid userId, string name, CancellationToken ct);
}
