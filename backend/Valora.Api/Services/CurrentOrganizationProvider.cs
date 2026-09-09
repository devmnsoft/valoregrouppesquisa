using System.Security.Claims;
using Valora.Application.Common;

namespace Valora.Api.Services;

public sealed class CurrentOrganizationProvider(
    ICurrentRequestContext requestContext,
    ILogger<CurrentOrganizationProvider> logger) : ICurrentOrganizationProvider {
    public CurrentOrganizationContext GetCurrent() {
        var current = requestContext.GetCurrent();
        if (current.EffectiveOrganizationId is { } id && id != Guid.Empty)
            return CurrentOrganizationContext.Resolved(id, current.IsGlobalAdministrator ? "selected-session" : "canonical-claim");

        logger.LogWarning("Contexto de organização ausente. UserId={UserId} SessionId={SessionId} IsGlobalAdministrator={IsGlobalAdministrator}",
            current.UserId, current.SessionId, current.IsGlobalAdministrator);
        return CurrentOrganizationContext.Unresolved();
    }
}
