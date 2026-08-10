using System.ComponentModel.DataAnnotations;

namespace Valora.Web.Models;

public sealed class ContactRequest
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Informe um nome válido.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail corporativo.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(160)]
    public string Email { get; init; } = string.Empty;

    [Phone(ErrorMessage = "Informe um telefone válido.")]
    [StringLength(24)]
    public string? Phone { get; init; }

    [Required(ErrorMessage = "Selecione o assunto.")]
    public string Subject { get; init; } = string.Empty;

    [Required(ErrorMessage = "Conte brevemente como podemos ajudar.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "A mensagem deve ter entre 10 e 2.000 caracteres.")]
    public string Message { get; init; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "É necessário aceitar o aviso de privacidade.")]
    public bool PrivacyAccepted { get; init; }
}
