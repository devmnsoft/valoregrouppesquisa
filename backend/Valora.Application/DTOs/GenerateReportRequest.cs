using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public sealed record GenerateReportRequest(
    [property: Required(ErrorMessage = "Escolha o formato do relatório."),
     RegularExpression("^(html|csv)$", ErrorMessage = "Escolha um formato de relatório válido.")]
    string Format = "html",
    Guid? ReportDefinitionId = null);
