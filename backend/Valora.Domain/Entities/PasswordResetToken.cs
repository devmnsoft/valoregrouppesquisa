namespace Valora.Domain.Entities;

public sealed record PasswordResetToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string TokenHash { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime? UsedAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
