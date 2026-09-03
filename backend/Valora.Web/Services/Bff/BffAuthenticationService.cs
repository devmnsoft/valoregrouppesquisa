using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;

namespace Valora.Web.Services.Bff;

public sealed class BffAuthenticationService(IBffApiClient api, IDistributedBffSessionStore sessions, ILogger<BffAuthenticationService> logger)
{
    public async Task<BffSafeSession> SignInAsync(HttpContext context, string endpoint, object request,
        CancellationToken cancellationToken, bool isPersistent = false)
    {
        var result = await api.PostAuthenticationAsync(endpoint, request, CorrelationId(context), cancellationToken);
        var ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var safe = new BffSafeSession(result.User, result.Organization, result.Plan, result.AccessContext);
        await sessions.SetAsync(ticket, new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken,
            result.RefreshTokenExpiresAt, safe), cancellationToken);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new Claim(ClaimTypes.Name, result.User.Name),
            new Claim(ClaimTypes.Email, result.User.Email),
            new Claim(ClaimTypes.Role, result.User.Role),
            new Claim("bff_ticket", ticket)
        };
        claims.AddRange(result.AccessContext.Roles.Select(value => new Claim(ClaimTypes.Role, value)));
        claims.AddRange(result.AccessContext.Permissions.Select(value => new Claim("permission", value)));
        claims.AddRange(result.AccessContext.EnabledModules.Select(value => new Claim("module", value)));
        claims.AddRange(result.AccessContext.Capabilities.Select(value => new Claim("capability", value)));
        claims.AddRange(result.AccessContext.Scopes.Select(value => new Claim("scope", value)));
        claims.Add(new Claim("subscription_status", result.AccessContext.SubscriptionStatus));
        AddContextClaims(claims, result);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            CookieProperties(result.RefreshTokenExpiresAt, isPersistent));
        return safe;
    }

    public async Task<BffServerSession?> GetAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        if (ticket is null) return null;
        var session = await sessions.GetAsync(ticket, cancellationToken);
        if (session is null) return null;
        if (session.SessionVersion == BffServerSession.CurrentSessionVersion
            && session.SafeSession.PayloadVersion == BffSafeSession.CurrentPayloadVersion
            && session.SafeSession.AccessContext.ContextVersion == BffAccessContext.CurrentContextVersion)
            return session;

        logger.LogInformation("Rehydrating stale BFF access context. UserId={UserId} Role={Role} OrganizationId={OrganizationId} PlanCode={PlanCode} ModuleCount={ModuleCount} PermissionCount={PermissionCount} ContextVersion={ContextVersion} CorrelationId={CorrelationId}",
            session.SafeSession.User.Id, session.SafeSession.User.Role, session.SafeSession.AccessContext.OrganizationId,
            session.SafeSession.AccessContext.PlanCode, session.SafeSession.AccessContext.EnabledModules.Count,
            session.SafeSession.AccessContext.Permissions.Count, session.SafeSession.AccessContext.ContextVersion, CorrelationId(context));
        try
        {
            await RefreshAsync(context, cancellationToken);
            return await sessions.GetAsync(ticket, cancellationToken);
        }
        catch (BffApiUnavailableException exception)
        {
            logger.LogWarning(exception, "API unavailable while rehydrating BFF session; preserving the authenticated session. UserId={UserId} CorrelationId={CorrelationId}",
                session.SafeSession.User.Id, CorrelationId(context));
            return session;
        }
        catch (BffApiException exception) when (exception.StatusCode is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden)
        {
            logger.LogWarning(exception, "API rejected access-context rehydration without invalidating credentials; preserving the session. Status={Status} UserId={UserId} CorrelationId={CorrelationId}",
                (int)exception.StatusCode, session.SafeSession.User.Id, CorrelationId(context));
            return session;
        }
        catch (BffApiException exception)
        {
            logger.LogWarning(exception, "Unable to rehydrate incompatible BFF session. UserId={UserId} CorrelationId={CorrelationId}", session.SafeSession.User.Id, CorrelationId(context));
            await sessions.RemoveAsync(ticket, cancellationToken);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return null;
        }
    }

    public async Task<BffSafeSession?> RefreshAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        if (ticket is null) return null;
        var current = await sessions.GetAsync(ticket, cancellationToken);
        if (current is null) return null;
        var result = await api.PostAuthenticationAsync("/api/v1/auth/refresh",
            new { refreshToken = current.RefreshToken }, CorrelationId(context), cancellationToken);
        var safe = new BffSafeSession(result.User, result.Organization, result.Plan, result.AccessContext);
        await sessions.SetAsync(ticket, new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken,
            result.RefreshTokenExpiresAt, safe), cancellationToken);
        await RenewCookieAsync(context, ticket, result, cancellationToken);
        return safe;
    }

    public async Task SignOutAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        if (ticket is not null)
        {
            var session = await sessions.GetAsync(ticket, cancellationToken);
            try
            {
                if (session is not null)
                    await api.PostAsync("/api/v1/auth/logout", new { refreshToken = session.RefreshToken }, session.AccessToken, cancellationToken);
            }
            catch (Exception exception) when (exception is BffApiException or BffApiUnavailableException)
            {
                logger.LogWarning(exception, "Remote logout could not be completed; clearing the local BFF session. CorrelationId={CorrelationId}", CorrelationId(context));
            }
            finally
            {
                await sessions.RemoveAsync(ticket, cancellationToken);
            }
        }
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static string CorrelationId(HttpContext context) =>
        context.Request.Headers.TryGetValue("X-Correlation-Id", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : context.TraceIdentifier;

    private static async Task RenewCookieAsync(HttpContext context, string ticket, BffAuthenticationResult result, CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.User.Id.ToString()), new(ClaimTypes.Name, result.User.Name),
            new(ClaimTypes.Email, result.User.Email), new(ClaimTypes.Role, result.User.Role), new("bff_ticket", ticket),
            new("subscription_status", result.AccessContext.SubscriptionStatus)
        };
        claims.AddRange(result.AccessContext.Roles.Select(value => new Claim(ClaimTypes.Role, value)));
        claims.AddRange(result.AccessContext.Permissions.Select(value => new Claim("permission", value)));
        claims.AddRange(result.AccessContext.EnabledModules.Select(value => new Claim("module", value)));
        claims.AddRange(result.AccessContext.Capabilities.Select(value => new Claim("capability", value)));
        claims.AddRange(result.AccessContext.Scopes.Select(value => new Claim("scope", value)));
        AddContextClaims(claims, result);
        var authentication = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var wasPersistent = authentication.Properties?.IsPersistent == true;
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            CookieProperties(result.RefreshTokenExpiresAt, wasPersistent));
    }

    private static AuthenticationProperties CookieProperties(DateTimeOffset expiresAt, bool isPersistent) => new()
    {
        IsPersistent = isPersistent,
        ExpiresUtc = expiresAt,
        AllowRefresh = true
    };

    private static void AddContextClaims(ICollection<Claim> claims, BffAuthenticationResult result)
    {
        var organizationId = result.AccessContext.OrganizationId ?? result.Organization?.Id;
        if (organizationId is { } id && id != Guid.Empty)
        {
            claims.Add(new Claim("organization_id", id.ToString()));
            claims.Add(new Claim("tenant_id", id.ToString()));
        }
        claims.Add(new Claim("session_id", result.SessionId.ToString()));
    }
}
