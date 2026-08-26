using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Valora.Web.Models.ViewModels;

public sealed class GenerateIntelligenceViewModel
{
    [Required(ErrorMessage="Selecione a organização.")] [Display(Name="Organização")] public Guid? OrganizationId { get; set; }
    [Required(ErrorMessage="Selecione o diagnóstico.")] [Display(Name="Diagnóstico")] public Guid? DiagnosticId { get; set; }
    [Required(ErrorMessage="Selecione o resultado.")] [Display(Name="Resultado")] public Guid? ResultId { get; set; }
    [Required(ErrorMessage="Selecione o tipo de análise.")] [Display(Name="Tipo de análise")] public string AnalysisType { get; set; } = "insights";
    public IReadOnlyList<SelectListItem> Organizations { get; init; } = [];
    public IReadOnlyList<SelectListItem> Diagnostics { get; init; } = [];
    public IReadOnlyList<SelectListItem> Results { get; init; } = [];
    public IReadOnlyList<SelectListItem> AnalysisTypes { get; init; } = [new("Insights organizacionais","insights"),new("Resumo executivo","executive_summary"),new("Riscos e oportunidades","risks")];
}

public sealed class RejectAiInsightViewModel
{
    [Required] public Guid InsightId { get; set; }
    [Required(ErrorMessage="Informe o motivo da rejeição.")] [MinLength(10)] [MaxLength(1000)] public string Reason { get; set; } = "";
}
