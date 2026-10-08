using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

// Classe (não record) de propósito: em .NET, records somente honram metadados de validação
// declarados nos parâmetros do construtor primário, invisíveis ao runtime Validator.
// Como classe, o metadata fica visível tanto ao MVC quanto ao Validator.ValidateObject.
public sealed class GenerateReportRequest {
    [Required(ErrorMessage = "Escolha o formato do relatório."),
     RegularExpression("^(html|csv)$", ErrorMessage = "Escolha um formato de relatório válido.")]
    public string Format { get; init; } = "html";

    public Guid? ReportDefinitionId { get; init; }

    public GenerateReportRequest() { }

    public GenerateReportRequest(string format) {
        Format = format;
    }
}
