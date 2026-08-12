using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Valora.Web.Services.Bff;

public sealed class BffAuthenticationService(IBffApiClient api, IDistributedBffSessionStore sessions)
{
    public async Task<BffSafeSession> SignInAsync(HttpContext context, string endpoint, object request, CancellationToken cancellationToken)
    {
        var result = await api.PostAuthenticationAsync(endpoint, request, CorrelationId(context), cancellationToken);
        var ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var safe = new BffSafeSession(result.User, result.Organization, result.Plan);
        await sessions.SetAsync(ticket, new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken,
            result.RefreshTokenExpiresAt, safe), cancellationToken);

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

    public async Task<BffServerSession?> GetAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        return ticket is null ? null : await sessions.GetAsync(ticket, cancellationToken);
    }

    public async Task<BffSafeSession?> RefreshAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        if (ticket is null) return null;
        var current = await sessions.GetAsync(ticket, cancellationToken);
        if (current is null) return null;
        var result = await api.PostAuthenticationAsync("/api/v1/auth/refresh",
            new { refreshToken = current.RefreshToken }, CorrelationId(context), cancellationToken);
        var safe = new BffSafeSession(result.User, result.Organization, result.Plan);
        await sessions.SetAsync(ticket, new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken,
            result.RefreshTokenExpiresAt, safe), cancellationToken);
        return safe;
    }

    public async Task SignOutAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var ticket = context.User.FindFirstValue("bff_ticket");
        if (ticket is not null)
        {
            var session = await sessions.GetAsync(ticket, cancellationToken);
            if (session is not null)
                await api.PostAsync("/api/v1/auth/logout", new { refreshToken = session.RefreshToken }, session.AccessToken, cancellationToken);
            await sessions.RemoveAsync(ticket, cancellationToken);
        }
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static string CorrelationId(HttpContext context) =>
        context.Request.Headers.TryGetValue("X-Correlation-Id", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : context.TraceIdentifier;
}
