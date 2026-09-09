using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Valora.Web.Services.Bff;

public sealed class PublicAccessSessionStore(
    IDataProtectionProvider dataProtection,
    IDistributedCache cache,
    IWebHostEnvironment environment)
{
    private const string CookieName = "Valora.PublicAccess";
    private readonly IDataProtector protector = dataProtection.CreateProtector("Valora.Web.PublicAccess.v1");

    public async Task WriteAsync(HttpContext context, string scope, Guid resourceId, string token, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(20);
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var payload = protector.Protect(JsonSerializer.Serialize(new Session(scope, resourceId, token, expiresAt)));
        await cache.SetStringAsync(CacheKey(sessionId), payload,
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt }, cancellationToken);
        context.Response.Cookies.Append(CookieName, protector.Protect(sessionId), new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment() || context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/bff/public",
            Expires = expiresAt,
            IsEssential = true
        });
    }

    public async Task<string?> ReadAsync(HttpContext context, string scope, Guid resourceId, CancellationToken cancellationToken)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var cookie)) return null;
        try
        {
            var sessionId = protector.Unprotect(cookie);
            var protectedPayload = await cache.GetStringAsync(CacheKey(sessionId), cancellationToken);
            if (protectedPayload is null) return null;
            var session = JsonSerializer.Deserialize<Session>(protector.Unprotect(protectedPayload));
            return session is not null && session.Scope == scope && session.ResourceId == resourceId &&
                   session.ExpiresAt > DateTimeOffset.UtcNow ? session.Token : null;
        }
        catch (CryptographicException)
        {
            context.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/bff/public" });
            return null;
        }
    }

    private static string CacheKey(string sessionId) => $"public-access:{sessionId}";
    private sealed record Session(string Scope, Guid ResourceId, string Token, DateTimeOffset ExpiresAt);
}
