using Valora.Application.ReadModels;

namespace Valora.Application.Contracts;

public interface IRefreshTokenRepository
{
    Task<Guid> CreateFamilyAsync(Guid sessionId);
    Task CreateAsync(Guid id, Guid familyId, Guid sessionId, Guid userId, Guid organizationId, string tokenHash, DateTimeOffset expiresAt);
    Task<RefreshTokenUseResult> RotateAsync(string currentHash, Guid replacementId, string replacementHash, DateTimeOffset replacementExpiresAt);
    Task RevokeByHashAsync(string tokenHash, string reason);
}
