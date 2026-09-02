using System.ComponentModel.DataAnnotations;

namespace Valora.Web.Models.ViewModels;

public sealed class EvolutionCycleViewModel
{
    [Required(ErrorMessage = "Informe um nome para o ciclo.")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "Use entre 3 e 160 caracteres.")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Descreva o objetivo do ciclo.")]
    [StringLength(1000)]
    public string Summary { get; init; } = string.Empty;

    [Range(0, 100, ErrorMessage = "O baseline deve estar entre 0 e 100.")]
    public decimal? BaselineScore { get; init; }

    [Range(0, 100, ErrorMessage = "A meta deve estar entre 0 e 100.")]
    public decimal? TargetScore { get; init; }

    [Required(ErrorMessage = "Informe a data inicial.")]
    [DataType(DataType.Date)]
    public DateTime PeriodStart { get; init; }

    [DataType(DataType.Date)]
    public DateTime? PeriodEnd { get; init; }

    [Required(ErrorMessage = "Registre a evidência inicial.")]
    [StringLength(2000)]
    public string EvidenceSummary { get; init; } = string.Empty;
}
