namespace Valora.Application.Common;

public sealed record CurrentRequestContext(
    Guid? UserId,
    Guid? SessionId,
    Guid? OrganizationId,
    Guid? SelectedOrganizationId,
    bool IsGlobalAdministrator,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Scopes,
    string SubscriptionStatus,
    long AccessVersion)
{
    public bool IsOrganizationResolved => EffectiveOrganizationId is not null;

    public Guid? EffectiveOrganizationId => IsGlobalAdministrator
        ? SelectedOrganizationId
        : OrganizationId;

    public Guid RequireUserId() => UserId is { } id && id != Guid.Empty
        ? id
        : throw new UnauthorizedAccessException("Sua sessão precisa ser renovada.");

    public Guid RequireOrganizationId() => EffectiveOrganizationId is { } id && id != Guid.Empty
        ? id
        : throw new UnauthorizedAccessException(CurrentOrganizationContext.RequiredMessage);
}

public interface ICurrentRequestContext
{
    CurrentRequestContext GetCurrent();
}
