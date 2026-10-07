namespace Valora.Application.ReadModels;

public sealed record UserRecord(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Email,
    string Status,
    string? Phone,
    bool PasswordResetRequired,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string[] RoleCodes) {
    public object? DeletedAt { get; internal set; }
}
