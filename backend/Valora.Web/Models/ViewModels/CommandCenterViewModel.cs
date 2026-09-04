using System.ComponentModel.DataAnnotations;
using Valora.Application.Indicators;

namespace Valora.Web.Models.ViewModels;

public sealed class CommandCenterFilterViewModel
{
    [StringLength(40)] public string Period { get; set; } = "90d";
    [StringLength(60)] public string? Module { get; set; }
    [StringLength(20)] public string? Severity { get; set; }
    public Guid? OrganizationId { get; set; }
}

public sealed record CommandCenterViewModel(
    string Page,
    CommandCenterFilterViewModel Filter,
    IReadOnlyList<IndicatorDto> Metrics,
    IReadOnlyList<IndicatorTargetDto> Targets,
    IReadOnlyList<IndicatorAlertDto> Alerts,
    IReadOnlyDictionary<Guid, TrendResult> Trends)
{
    public int CriticalAlerts => Alerts.Count(x => x.Status != "resolved" && x.Severity == "critical");
    public int MetricsWithData => Metrics.Count(x => x.LatestValue.HasValue);
    public int InsufficientMetrics => Metrics.Count - MetricsWithData;
    public int WorseningMetrics => Trends.Count(x => x.Value.Trend == IndicatorTrend.Worsening);
}

public sealed class ResolveCommandCenterAlertViewModel
{
    [Required] public Guid AlertId { get; set; }
    [Required, StringLength(500, MinimumLength = 5)] public string ResolutionNote { get; set; } = string.Empty;
    [StringLength(20)] public string? ReturnSeverity { get; set; }
}
