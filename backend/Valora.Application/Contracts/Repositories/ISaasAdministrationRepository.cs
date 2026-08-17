namespace Valora.Application.Contracts;

public interface ISaasAdministrationRepository
{
    Task<IReadOnlyList<SaasGovernanceEvent>> ListGovernanceAsync(Guid organizationId, bool global, string? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    Task<SaasGovernanceEvent?> GetGovernanceAsync(Guid organizationId, bool global, Guid id, CancellationToken ct);
    Task<IReadOnlyList<SaasNotification>> ListNotificationsAsync(Guid organizationId, Guid userId, string? type, bool? unread, CancellationToken ct);
    Task<bool> MarkNotificationReadAsync(Guid organizationId, Guid userId, Guid id, CancellationToken ct);
    Task<int> MarkAllNotificationsReadAsync(Guid organizationId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<SaasHealthEvent>> ListHealthEventsAsync(CancellationToken ct);
}

public sealed record SaasGovernanceEvent(Guid Id, Guid? OrganizationId, Guid? UserId, string Module, string EntityType,
    Guid? EntityId, string Action, string? BeforeJson, string? AfterJson, string? Reason, string? CorrelationId,
    string Severity, DateTimeOffset CreatedAt);
public sealed record SaasNotification(Guid Id, string Type, string Title, string Message, string? RelatedModule,
    Guid? RelatedEntityId, DateTimeOffset? ReadAt, DateTimeOffset CreatedAt);
public sealed record SaasHealthEvent(Guid Id, string Component, string Status, string? Message, string? CorrelationId,
    DateTimeOffset CreatedAt);
