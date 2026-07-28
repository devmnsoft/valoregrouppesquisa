using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Valora.Web.Services.Bff;

public sealed class BffAuthenticationService(IBffApiClient api, IBffSessionStore sessions)
{
    public async Task<BffSafeSession> SignInAsync(HttpContext context, string endpoint, object request, CancellationToken cancellationToken)
    {
        var result = await api.PostAuthenticationAsync(endpoint, request, cancellationToken);
        var ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var safe = new BffSafeSession(result.User, result.Organization, result.Plan);
        sessions.Set(ticket, new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken,
            result.RefreshTokenExpiresAt, safe));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new Claim(ClaimTypes.Name, result.User.Name),
            new Claim(ClaimTypes.Email, result.User.Email),
            new Claim(ClaimTypes.Role, result.User.Role),
            new Claim("bff_ticket", ticket)
        };
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = result.RefreshTokenExpiresAt });
        return safe;
    }

    public bool TryGet(HttpContext context, out BffServerSession? session)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        return ticket is not null && sessions.TryGet(ticket, out session);
    }

    public async Task<BffSafeSession?> RefreshAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        if (ticket is null || !sessions.TryGet(ticket, out var current) || current is null) return null;
        var result = await api.PostAuthenticationAsync("/api/v1/auth/refresh",
            new { refreshToken = current.RefreshToken }, cancellationToken);
        var safe = new BffSafeSession(result.User, result.Organization, result.Plan);
        sessions.Set(ticket, new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken,
            result.RefreshTokenExpiresAt, safe));
        return safe;
    }

    public async Task SignOutAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        if (ticket is not null && sessions.TryGet(ticket, out var session) && session is not null)
        {
            await api.PostAsync("/api/v1/auth/logout", new { refreshToken = session.RefreshToken }, session.AccessToken, cancellationToken);
            sessions.Remove(ticket);
        }
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
