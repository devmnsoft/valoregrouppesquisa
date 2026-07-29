using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;

namespace Valora.Application.Services;

public sealed class AuthenticationSessionService(
    ISessionRepository sessions,
    IRefreshTokenRepository refreshTokens,
    IJwtTokenService jwt,
    IOptions<AuthenticationOptions> options) : IAuthenticationSessionService
{
    public async Task<TokenPair> CreateAsync(Guid userId, Guid organizationId, string email, string role,
        string locale, string? ipAddress = null, string? userAgent = null)
    {
        var now = DateTimeOffset.UtcNow;
        var refreshExpiresAt = now.AddDays(options.Value.RefreshTokenDays);
        var sessionId = await sessions.CreateAsync(userId, organizationId, refreshExpiresAt, HashNullable(ipAddress), userAgent);
        var familyId = await refreshTokens.CreateFamilyAsync(sessionId);
        var raw = GenerateToken();
        await refreshTokens.CreateAsync(Guid.NewGuid(), familyId, sessionId, userId, organizationId, Hash(raw), refreshExpiresAt);
        return Pair(userId, organizationId, sessionId, email, role, locale, raw, refreshExpiresAt);
    }

    public async Task<TokenPair> RefreshAsync(string rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken)) throw new UnauthorizedAccessException("Refresh token inválido.");
        var replacementRaw = GenerateToken();
        var replacementExpiresAt = DateTimeOffset.UtcNow.AddDays(options.Value.RefreshTokenDays);
        var result = await refreshTokens.RotateAsync(Hash(rawRefreshToken), Guid.NewGuid(), Hash(replacementRaw), replacementExpiresAt);
        if (result.Status != RefreshTokenUseStatus.Rotated || result.Current is null)
            throw new UnauthorizedAccessException(result.Status == RefreshTokenUseStatus.Reused
                ? "Refresh token reutilizado; sessão revogada." : "Refresh token inválido ou expirado.");
        return Pair(result.Current.UserId, result.Current.OrganizationId, result.Current.SessionId,
            result.Current.Email, result.Current.Role, result.Current.Locale, replacementRaw, replacementExpiresAt);
    }

    public async Task LogoutAsync(Guid userId, string rawRefreshToken)
    {
        await refreshTokens.RevokeByHashAsync(Hash(rawRefreshToken), "logout");
    }

    public Task LogoutAllAsync(Guid userId) => sessions.RevokeAllAsync(userId, "logout_all");
    public async Task<IReadOnlyList<SessionDto>> ListAsync(Guid userId) => (await sessions.ListAsync(userId))
        .Select(x => new SessionDto(x.Id, x.CreatedAt, x.LastUsedAt, x.ExpiresAt)).ToArray();
    public Task RevokeAsync(Guid userId, Guid sessionId) => sessions.RevokeAsync(sessionId, userId, "user_revoked");

    private TokenPair Pair(Guid userId, Guid organizationId, Guid sessionId, string email, string role,
        string locale, string refreshToken, DateTimeOffset refreshExpiresAt)
    {
        var accessExpiresAt = DateTimeOffset.UtcNow.AddMinutes(options.Value.AccessTokenMinutes);
        return new TokenPair(sessionId, userId, organizationId,
            jwt.CreateToken(userId, organizationId, sessionId, email, role, locale),
            accessExpiresAt, refreshToken, refreshExpiresAt);
    }

    private static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string? HashNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : Hash(value);
}
