namespace Valora.Application.ReadModels;

public sealed record UserRecord(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Email,
    string Status,
    string? Phone,
    bool PasswordResetRequired,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string[] RoleCodes) {
    public object DeletedAt { get; internal set; }
}