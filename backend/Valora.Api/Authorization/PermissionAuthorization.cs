using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Valora.Application.Contracts;

namespace Valora.Api.Authorization;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(IPermissionService permissions) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return;
        var tenantValue=context.User.FindFirstValue("organization_id") ?? context.User.FindFirstValue("organizationId");
        if (!Guid.TryParse(tenantValue, out var organizationId)) return;
        if (await permissions.HasPermissionAsync(userId, requirement.Code, organizationId)) context.Succeed(requirement);
    }
}
