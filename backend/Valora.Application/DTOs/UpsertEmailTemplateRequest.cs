using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

// Classe (não record) de propósito: em .NET, records somente honram metadados de validação
// declarados nos parâmetros do construtor primário, invisíveis ao runtime Validator e à
// reflexão sobre propriedades. Como classe, o metadata fica visível a todos os mecanismos.
public sealed class UpsertEmailTemplateRequest
{
    public Guid? OrganizationId { get; set; }

    [Required, StringLength(80, MinimumLength = 2)] public string Code { get; set; } = "";
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = "";
    [Required, StringLength(200, MinimumLength = 2)] public string Subject { get; set; } = "";
    [Required, StringLength(100_000, MinimumLength = 3)] public string BodyHtml { get; set; } = "";
    [StringLength(100_000)] public string? BodyText { get; set; }
    [Required, RegularExpression("^(active|inactive)$")] public string Status { get; set; } = "active";
}
