namespace Valora.Application.Communication;

public sealed class EmailOptions
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool SmtpSecure { get; set; }
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromName { get; set; } = "Valora Group";
    public string FromEmail { get; set; } = "valoragroup@mnsoft.com.br";
}
