using System.Net.Mail;

namespace Valora.Application.Communication;

public sealed record EmailConfigurationStatus(bool Ok,bool Enabled,string Provider,bool FromEmailConfigured,bool SmtpHostConfigured,bool SmtpUserConfigured,bool SmtpPasswordConfigured,bool CanSend,IReadOnlyList<string> Errors);

public static class EmailConfigurationValidator
{
    public static EmailConfigurationStatus Validate(EmailOptions options)
    {
        var errors = new List<string>();
        var provider = string.IsNullOrWhiteSpace(options.Provider) ? "Smtp" : options.Provider.Trim();
        if (options.Enabled)
        {
            if (!provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase)) errors.Add("Email.Provider deve ser Smtp.");
            if (!IsEmail(options.FromEmail)) errors.Add("Email.FromEmail obrigatório e válido.");
            if (string.IsNullOrWhiteSpace(options.FromName)) errors.Add("Email.FromName obrigatório.");
            if (!string.IsNullOrWhiteSpace(options.ReplyTo) && !IsEmail(options.ReplyTo)) errors.Add("Email.ReplyTo inválido.");
            if (string.IsNullOrWhiteSpace(options.Smtp.Host)) errors.Add("Email.Smtp.Host obrigatório.");
            if (options.Smtp.Port <= 0) errors.Add("Email.Smtp.Port obrigatório.");
            if (string.IsNullOrWhiteSpace(options.Smtp.Username)) errors.Add("Email.Smtp.Username obrigatório.");
            if (string.IsNullOrWhiteSpace(options.Smtp.Password)) errors.Add("Email.Smtp.Password obrigatório.");
        }
        return new EmailConfigurationStatus(errors.Count == 0, options.Enabled, provider, IsEmail(options.FromEmail), !string.IsNullOrWhiteSpace(options.Smtp.Host), !string.IsNullOrWhiteSpace(options.Smtp.Username), !string.IsNullOrWhiteSpace(options.Smtp.Password), options.Enabled && errors.Count == 0, errors);
    }
    public static bool IsEmail(string? value) { try { return !string.IsNullOrWhiteSpace(value) && new MailAddress(value).Address == value.Trim(); } catch { return false; } }
}
