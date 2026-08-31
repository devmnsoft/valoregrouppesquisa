using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Benchmarks;

public sealed record BenchmarkCohortDto(Guid Id,string Name,string Description,string Segment,string Industry,string CompanySizeRange,string Region,int MinimumSampleSize,string Status,int MemberCount,DateTimeOffset UpdatedAt);
public sealed record BenchmarkSnapshotDto(Guid Id,string SourceType,Guid SourceId,DateOnly PeriodStart,DateOnly PeriodEnd,decimal MaturityScore,string ConfidenceLevel,int SampleSize,string MetadataJson,DateTimeOffset CreatedAt);
public sealed record BenchmarkMetricDto(string MetricKey,string MetricName,string? Dimension,decimal OrganizationValue,decimal? BenchmarkValue,decimal? Difference,decimal? Percentile,string ConfidenceLevel,int SampleSize,string DataKind);
public sealed record BenchmarkComparisonDto(Guid Id,string ComparisonType,string Segment,string Criterion,DateOnly PeriodStart,DateOnly PeriodEnd,bool IsAvailable,string ConfidenceLevel,int SampleSize,IReadOnlyList<string> Limitations,IReadOnlyList<BenchmarkMetricDto> Metrics);
public sealed record BenchmarkInsightDto(Guid Id,string InsightType,string Title,string Description,string EvidenceSummary,string ConfidenceLevel,string Status,Guid? ActionId,DateTimeOffset CreatedAt);
public sealed record BenchmarkPrivacyRuleDto(Guid Id,int MinimumSampleSize,int KAnonymity,string Status,string? SuppressionReason,DateTimeOffset UpdatedAt);
public sealed record BenchmarkDashboardDto(BenchmarkSnapshotDto? Current,IReadOnlyList<BenchmarkSnapshotDto> History,IReadOnlyList<BenchmarkInsightDto> Insights,IReadOnlyList<string> Warnings);

public sealed class CreateBenchmarkCohortRequest {
    [Required,StringLength(160)] public string Name {get;init;}=""; [StringLength(1000)] public string Description {get;init;}="";
    [Required,StringLength(100)] public string Segment {get;init;}=""; [Required,StringLength(100)] public string Industry {get;init;}="";
    [Required,StringLength(80)] public string CompanySizeRange {get;init;}=""; [Required,StringLength(100)] public string Region {get;init;}="";
    [Range(5,10000)] public int MinimumSampleSize {get;init;}=5;
}
public sealed class GenerateBenchmarkSnapshotRequest {
    [Required,RegularExpression("^(diagnostic|indicators|evolution|unit)$")] public string SourceType {get;init;}="diagnostic";
    [Required] public Guid SourceId {get;init;} [Required] public DateOnly PeriodStart {get;init;} [Required] public DateOnly PeriodEnd {get;init;}
    [Range(0,100)] public decimal MaturityScore {get;init;} [Range(1,1000000)] public int SampleSize {get;init;}
    [Required,RegularExpression("^(low|medium|high)$")] public string ConfidenceLevel {get;init;}="low";
    public string MetadataJson {get;init;}="{}";
}
public sealed class BenchmarkComparisonRequest {
    [Required,RegularExpression("^(history|units|cohort)$")] public string ComparisonType {get;init;}="history";
    public Guid? CohortId {get;init;} [Required,StringLength(100)] public string Segment {get;init;}="";
    [Required,StringLength(120)] public string Criterion {get;init;}="maturity"; [Required] public DateOnly PeriodStart {get;init;} [Required] public DateOnly PeriodEnd {get;init;}
    [StringLength(120)] public string? Dimension {get;init;} [StringLength(120)] public string? Indicator {get;init;}
}
public sealed class BenchmarkExportRequest { [Required] public Guid ComparisonId {get;init;} [Required,RegularExpression("^(csv|json|pdf)$")] public string Format {get;init;}="csv"; }

public interface IBenchmarkCohortRepository { Task<IReadOnlyList<BenchmarkCohortDto>> List(Guid organizationId,CancellationToken ct); Task<Guid> Create(Guid organizationId,Guid userId,CreateBenchmarkCohortRequest request,CancellationToken ct); }
public interface IBenchmarkSnapshotRepository { Task<IReadOnlyList<BenchmarkSnapshotDto>> List(Guid organizationId,CancellationToken ct); Task<Guid> Create(Guid organizationId,Guid userId,GenerateBenchmarkSnapshotRequest request,CancellationToken ct); }
public interface IBenchmarkMetricRepository { Task<IReadOnlyList<BenchmarkMetricDto>> Compare(Guid organizationId,Guid requestId,BenchmarkComparisonRequest request,CancellationToken ct); }
public interface IBenchmarkComparisonRepository { Task<Guid> CreateRequest(Guid organizationId,Guid userId,BenchmarkComparisonRequest request,CancellationToken ct); Task Complete(Guid organizationId,Guid requestId,bool available,string confidence,int sampleSize,IReadOnlyList<string> limitations,CancellationToken ct); Task<BenchmarkComparisonDto?> Get(Guid organizationId,Guid requestId,CancellationToken ct); }
public interface IBenchmarkInsightRepository { Task<IReadOnlyList<BenchmarkInsightDto>> List(Guid organizationId,CancellationToken ct); Task<Guid> ConvertToAction(Guid organizationId,Guid insightId,Guid userId,CancellationToken ct); }
public interface IBenchmarkPrivacyRepository { Task<BenchmarkPrivacyRuleDto> Get(Guid organizationId,CancellationToken ct); Task AuditBlocked(Guid organizationId,Guid userId,string reason,CancellationToken ct); }
public interface IBenchmarkExportRepository { Task<Guid> Create(Guid organizationId,Guid userId,BenchmarkExportRequest request,CancellationToken ct); }
