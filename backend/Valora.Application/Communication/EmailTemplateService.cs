using Valora.Application.Security;
using Microsoft.Extensions.Logging;
namespace Valora.Application.Communication;
public sealed class EmailTemplateService(ILogger<EmailTemplateService>? logger = null)
{
    public const string ResultSubject = "Seu diagnóstico gratuito Valora Pulse está pronto";
    public string BuildPasswordResetTemplate(string maskedEmail, string resetUrl){ logger?.LogInformation("Password reset template generated. Email={Email}", LogSanitizer.MaskEmail(maskedEmail)); return $"Solicitação de recuperação para {maskedEmail}. Acesse: {resetUrl}"; }
    public static string BuildResultTemplate(Guid responseId, string? message, bool includeCertificate)
    {
        var note = string.IsNullOrWhiteSpace(message) ? "Confira a recomendação inicial e o resumo por dimensão no link do resultado." : message.Trim();
        return $"Valora Pulse™ | Valora Group\n\nOlá, participante. Seu diagnóstico gratuito Valora Pulse está pronto.\nPesquisa: Diagnóstico gratuito Valora Insight\nPontuação total e nível de maturidade: disponíveis no resultado oficial.\nResumo por dimensão: cultura, governança, liderança, pessoas e crescimento.\nRecomendação inicial: {note}\nVer resultado: /results/{responseId}\nValidar certificado: /certificates/validate/VALORA-{responseId:N}\nCTA WhatsApp: fale com a Valora para conhecer os planos completos.\n{(includeCertificate?"Certificado de Diagnóstico incluído quando disponível. Código de validação público gerado.":string.Empty)}\nRodapé LGPD: usamos seus dados apenas para entregar o diagnóstico, comunicação transacional e validação do certificado.";
    }
}
