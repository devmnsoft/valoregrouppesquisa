namespace Valora.Domain.Entities;

public sealed class CertificateValidation
{
    public Guid Id { get; set; }
    public Guid CertificateId { get; set; }
    public string ValidationCode { get; set; } = string.Empty;
    public string ValidationCodeHash { get; set; } = string.Empty;
    public string Status { get; set; } = "valid";
    public DateTimeOffset? ValidatedAt { get; set; }
    public string? ValidationIpHash { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
