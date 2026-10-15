using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models.Binding;

namespace Valora.Web.Models.ViewModels;

public sealed class EvolutionCycleViewModel {
    public Guid? DiagnosticId { get; init; }
    public Guid? ResultId { get; init; }
    public Guid? SourceSurveyId { get; init; }
    [RegularExpression("manual|calculated")] public string ReferenceMode { get; init; } = "manual";

    [Required(ErrorMessage = "Informe um nome para o ciclo.")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "Use entre 3 e 160 caracteres.")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Descreva o objetivo do ciclo.")]
    [StringLength(1000)]
    public string Summary { get; init; } = string.Empty;

    [Range(0, 100, ErrorMessage = "O baseline deve estar entre 0 e 100.")]
    [ModelBinder(BinderType = typeof(EvolutionNumberModelBinder))]
    public decimal? BaselineScore { get; init; }

    [Range(0, 100, ErrorMessage = "A meta deve estar entre 0 e 100.")]
    [ModelBinder(BinderType = typeof(EvolutionNumberModelBinder))]
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

public sealed class EvolutionSnapshotViewModel {
    [Required(ErrorMessage = "Descreva as evidências observadas."), StringLength(2000)] public string Evidence { get; init; } = string.Empty;
    [Required(ErrorMessage = "Registre uma interpretação humana."), StringLength(2000)] public string Interpretation { get; init; } = string.Empty;
    [Required(ErrorMessage = "Informe a recomendação ou o próximo passo."), StringLength(2000)] public string Recommendation { get; init; } = string.Empty;
}
