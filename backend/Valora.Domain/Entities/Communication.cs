using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record Communication : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string RecipientHash { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public CommunicationStatus Status { get; init; } = CommunicationStatus.Pending;
    public DateTime? SentAt { get; init; }
}
