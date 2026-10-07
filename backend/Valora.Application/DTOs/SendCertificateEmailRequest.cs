using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public sealed record SendCertificateEmailRequest(
    [Required(ErrorMessage = "Informe o e-mail do destinatário."), EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    string ToEmail);
