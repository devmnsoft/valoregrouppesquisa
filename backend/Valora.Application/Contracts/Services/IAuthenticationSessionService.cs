using Valora.Application.DTOs;

namespace Valora.Application.Contracts;

public interface IAuthenticationSessionService
{
    Task<TokenPair> CreateAsync(Guid userId, Guid organizationId, string email, string role, string locale, string? ipAddress = null, string? userAgent = null);
    Task<TokenPair> RefreshAsync(string rawRefreshToken);
    Task LogoutAsync(Guid userId, string rawRefreshToken);
    Task LogoutAllAsync(Guid userId);
    Task<IReadOnlyList<SessionDto>> ListAsync(Guid userId);
    Task RevokeAsync(Guid userId, Guid sessionId);
}
