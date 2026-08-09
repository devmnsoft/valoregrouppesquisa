using System.Text;

namespace Valora.Application.Communication;

public static class WhatsAppLinkBuilder
{
    public const string OfficialNumber = "5591992545353";

    public static string Build(string company, string user, string module, string subject, string? relevantUrl = null, string? correlationId = null)
    {
        static string Clean(string value) => value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        var message = new StringBuilder("Olá, Valora. ")
            .Append("Preciso de atendimento sobre ").Append(Clean(subject)).Append(" da empresa ").Append(Clean(company)).Append(". ")
            .Append("Usuário: ").Append(Clean(user)).Append(". Módulo: ").Append(Clean(module)).Append('.');
        if (!string.IsNullOrWhiteSpace(correlationId)) message.Append(" Código de atendimento: ").Append(Clean(correlationId)).Append('.');
        if (Uri.TryCreate(relevantUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps) message.Append(" Link: ").Append(uri);
        return $"https://wa.me/{OfficialNumber}?text={Uri.EscapeDataString(message.ToString())}";
    }
}
