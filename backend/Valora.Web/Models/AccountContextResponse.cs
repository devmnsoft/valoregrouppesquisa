namespace Valora.Web.Models;

public sealed record AccountContextResponse(
    Guid UserId,
    string UserName,
    string UserEmail,
    string UserInitials,
    string PrimaryRole,
    IReadOnlyList<string> RoleCodes,
    Guid? OrganizationId,
    string? OrganizationName,
    string? OrganizationLogoUrl,
    string? PlanCode,
    string? PlanName,
    string SubscriptionStatus,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Scopes);
