using Microsoft.Extensions.Logging;
using Valora.Application.Security;

namespace Valora.Application.Communication;

public sealed class EmailTemplateService(ILogger<EmailTemplateService> logger)
{
    public string BuildPasswordResetTemplate(string maskedEmail, string resetUrl)
    {
        logger.LogInformation("Password reset template generated. Email={Email}", LogSanitizer.MaskEmail(maskedEmail));
        return $"Use o link seguro para redefinir sua senha: {resetUrl}";
    }

    public string BuildResultTemplate(string participantName, string resultUrl)
    {
        logger.LogInformation("Result email template generated.");
        return $"Olá {participantName}, seu resultado está disponível em {resultUrl}";
    }
}
