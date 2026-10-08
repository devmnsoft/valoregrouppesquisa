using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Valora.Application.Common;
using Valora.Application.Contracts;

namespace Valora.Api.Authorization;

public sealed class PermissionAuthorizationHandler(IPermissionService permissions, ICurrentRequestContext requestContext) : AuthorizationHandler<PermissionRequirement> {
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement) {
        var current = requestContext.GetCurrent();
        if (current.UserId is not { } userId || userId == Guid.Empty) return;
        // Paridade com o handler do BFF/Web: o papel de plataforma opera com o catálogo
        // completo (ValoraPermissions.All no contexto de login); o escopo de organização
        // continua sendo exigido pela resolução de contexto de cada endpoint.
        if (current.IsGlobalAdministrator) {
            context.Succeed(requirement);
            return;
        }
        var organizationId = current.OrganizationId;
        if (organizationId is null || organizationId == Guid.Empty) return;
        if (await permissions.HasPermissionAsync(userId, requirement.Code, organizationId)) context.Succeed(requirement);
    }
}
