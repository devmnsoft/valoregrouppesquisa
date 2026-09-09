using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Valora.Application.Contracts;
using Valora.Application.Common;

namespace Valora.Api.Authorization;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(IPermissionService permissions, ICurrentRequestContext requestContext) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var current = requestContext.GetCurrent();
        if (current.UserId is not { } userId || userId == Guid.Empty) return;
        var organizationId = current.IsGlobalAdministrator
            ? current.SelectedOrganizationId ?? current.OrganizationId
            : current.OrganizationId;
        if (organizationId is null || organizationId == Guid.Empty) return;
        if (await permissions.HasPermissionAsync(userId, requirement.Code, organizationId)) context.Succeed(requirement);
    }
}
