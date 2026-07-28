using Valora.Application.ReadModels;

namespace Valora.Application.Contracts;

public interface ISessionRepository
{
    Task<Guid> CreateAsync(Guid userId, Guid organizationId, DateTimeOffset expiresAt, string? ipHash, string? userAgent);
    Task<AuthenticationSessionRecord?> GetAsync(Guid sessionId);
    Task<IReadOnlyList<AuthenticationSessionRecord>> ListAsync(Guid userId);
    Task RevokeAsync(Guid sessionId, Guid userId, string reason);
    Task RevokeAllAsync(Guid userId, string reason);
}
