using System.ComponentModel.DataAnnotations;
using Valora.Application.Benchmarks;
namespace Valora.Web.Models.ViewModels;
public sealed record BenchmarksViewModel(BenchmarkSnapshotDto? Current,IReadOnlyList<BenchmarkSnapshotDto> History,IReadOnlyList<BenchmarkCohortDto> Cohorts,IReadOnlyList<BenchmarkInsightDto> Insights,BenchmarkPrivacyRuleDto Privacy,string Section);
public sealed class BenchmarkCompareForm : IValidatableObject
{
    private static readonly string[] ComparisonTypes = ["history", "units", "cohort"];
    private static readonly string[] Criteria = ["maturity", "dimension", "indicator", "evolution"];

    [Required]
    public string ComparisonType { get; set; } = "history";
    public Guid? CohortId { get; set; }
    [Required, StringLength(100)]
    public string Segment { get; set; } = "Geral";
    [Required]
    public string Criterion { get; set; } = "maturity";
    [Required]
    public DateOnly PeriodStart { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));
    [Required]
    public DateOnly PeriodEnd { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string? Dimension { get; set; }
    public string? Indicator { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ComparisonTypes.Contains(ComparisonType, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("Selecione um tipo de comparação válido.", [nameof(ComparisonType)]);
        if (!Criteria.Contains(Criterion, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("Selecione um critério válido.", [nameof(Criterion)]);
        if (PeriodEnd < PeriodStart)
            yield return new ValidationResult("A data final deve ser igual ou posterior à data inicial.", [nameof(PeriodEnd)]);
        if (ComparisonType.Equals("cohort", StringComparison.OrdinalIgnoreCase) && CohortId is null)
            yield return new ValidationResult("Selecione uma coorte para a comparação anônima.", [nameof(CohortId)]);
        if (Criterion.Equals("dimension", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Dimension))
            yield return new ValidationResult("Informe a dimensão que será comparada.", [nameof(Dimension)]);
        if (Criterion.Equals("indicator", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Indicator))
            yield return new ValidationResult("Informe o indicador que será comparado.", [nameof(Indicator)]);
    }
}
public sealed class BenchmarkCohortForm { [Required,StringLength(160)] public string Name {get;set;}=""; [StringLength(1000)] public string Description {get;set;}=""; [Required] public string Segment {get;set;}=""; [Required] public string Industry {get;set;}=""; [Required] public string CompanySizeRange {get;set;}=""; [Required] public string Region {get;set;}=""; [Range(5,10000)] public int MinimumSampleSize {get;set;}=5; }
