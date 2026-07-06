namespace Valora.Domain.Entities;

public sealed class PrivacyRequest
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public string RequesterEmailHash { get; set; } = string.Empty;
    public string RequesterEmailMasked { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RequestType { get; set; } = "data_export";
    public string Protocol { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public string? Description { get; set; }
    public Guid? ResponseId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? HandledBy { get; set; }
    public string? ResultJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
