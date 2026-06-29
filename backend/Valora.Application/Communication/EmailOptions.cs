namespace Valora.Application.Communication;

public sealed class EmailOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Smtp";
    public string FromName { get; set; } = "Valora Group";
    public string FromEmail { get; set; } = "valoragroup@mnsoft.com.br";
    public string ReplyTo { get; set; } = "valoragroup@mnsoft.com.br";
    public SmtpOptions Smtp { get; set; } = new();

    public string SmtpHost { get => Smtp.Host; set => Smtp.Host = value ?? string.Empty; }
    public int SmtpPort { get => Smtp.Port; set => Smtp.Port = value; }
    public bool SmtpSecure { get => Smtp.UseSsl; set => Smtp.UseSsl = value; }
    public string SmtpUser { get => Smtp.Username; set => Smtp.Username = value ?? string.Empty; }
    public string SmtpPassword { get => Smtp.Password; set => Smtp.Password = value ?? string.Empty; }
}
