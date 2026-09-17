using Microsoft.AspNetCore.Authorization;
using Valora.Application.Access;
using Valora.Application.Common;
using Valora.Application.Contracts;

namespace Valora.Web.Security;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(
    IPermissionService permissions,
    ICurrentRequestContext requestContext) : AuthorizationHandler<PermissionRequirement> {
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement) {
        var current = requestContext.GetCurrent();
        if (current.UserId is not { } userId || userId == Guid.Empty) return;
        if (current.IsGlobalAdministrator) {
            context.Succeed(requirement);
            return;
        }

        var organizationId = current.EffectiveOrganizationId ?? current.OrganizationId;
        if (organizationId is null || organizationId == Guid.Empty) return;
        if (await permissions.HasPermissionAsync(userId, requirement.Code, organizationId))
            context.Succeed(requirement);
    }
}

public static class PermissionAuthorizationExtensions {
    public static void RegisterValoraPermissionPolicies(this AuthorizationOptions options) {
        foreach (var permission in ValoraPermissions.All)
            options.AddPolicy(permission, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission)));
    }

    public static IServiceCollection AddValoraPermissionHandler(this IServiceCollection services) {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
