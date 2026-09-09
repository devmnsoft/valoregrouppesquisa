using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Services.Bff;

public sealed record BffSafeSession(BffUser User, BffOrganization? Organization, BffPlan? Plan, BffAccessContext AccessContext) {
    public const int CurrentPayloadVersion = 3;
    public int PayloadVersion { get; init; } = CurrentPayloadVersion;
    public BffEffectiveAccessSnapshot EffectiveAccessSnapshot => new(
        User, Organization, AccessContext.SelectedOrganizationId, Plan, AccessContext.SubscriptionStatus,
        AccessContext.Roles, AccessContext.Permissions, AccessContext.EnabledModules,
        AccessContext.Capabilities, AccessContext.Scopes, AccessContext.AccessVersion, AccessContext.GeneratedAt);
}

public sealed record BffAccessContext(
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions, IReadOnlyList<string> EnabledModules,
    IReadOnlyList<string> Capabilities, IReadOnlyList<string> Scopes, string SubscriptionStatus,
    Guid? OrganizationId, string? PlanCode) {
    public const int CurrentContextVersion = 3;
    public int ContextVersion { get; init; } = CurrentContextVersion;
    public Guid? SelectedOrganizationId { get; init; }
    public bool IsGlobalAdministrator { get; init; }
    public long AccessVersion { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed record BffEffectiveAccessSnapshot(
    BffUser User,
    BffOrganization? Organization,
    Guid? SelectedOrganizationId,
    BffPlan? Plan,
    string SubscriptionStatus,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Scopes,
    long AccessVersion,
    DateTimeOffset GeneratedAt);
