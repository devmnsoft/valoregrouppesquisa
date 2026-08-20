using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Valora.Web.Models;

namespace Valora.Web.Services.Bff;

public sealed record BffSafeSession(BffUser User, BffOrganization? Organization, BffPlan? Plan, BffAccessContext AccessContext);

public sealed record BffAccessContext(
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions, IReadOnlyList<string> EnabledModules,
    IReadOnlyList<string> Capabilities, IReadOnlyList<string> Scopes, string SubscriptionStatus,
    Guid? OrganizationId, string? PlanCode);
