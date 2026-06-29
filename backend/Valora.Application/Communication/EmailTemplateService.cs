using Valora.Application.Security;
using Microsoft.Extensions.Logging;
namespace Valora.Application.Communication;
public sealed class EmailTemplateService(ILogger<EmailTemplateService>? logger = null)
{
    public string BuildPasswordResetTemplate(string maskedEmail, string resetUrl){ logger?.LogInformation("Password reset template generated. Email={Email}", LogSanitizer.MaskEmail(maskedEmail)); return $"Solicitação de recuperação para {maskedEmail}. Acesse: {resetUrl}"; }
    public static string BuildResultTemplate(Guid responseId, string? message, bool includeCertificate) => $"Olá, seu resultado Valora Pulse está disponível. ResponseId: {responseId}. {(string.IsNullOrWhiteSpace(message)?string.Empty:message.Trim())} {(includeCertificate?"Certificado incluído quando disponível.":string.Empty)}";
}
