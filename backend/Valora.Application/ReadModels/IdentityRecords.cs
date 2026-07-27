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
    string[] RoleCodes);

public sealed record UserAuthenticationRecord(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Email,
    string PasswordHash,
    string Status,
    string? Phone,
    string RoleCodesCsv)
{
    public IReadOnlyList<string> RoleCodes => RoleCodesCsv
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed record PasswordResetTokenRecord(Guid Id, Guid UserId, DateTimeOffset ExpiresAt, DateTimeOffset? UsedAt);

public sealed record OrganizationRecord(
    Guid Id,
    string Name,
    string? PublicName,
    string Slug,
    string? Email,
    string? Phone,
    string Status,
    string DefaultLanguageCode,
    string TimeZone,
    string OnboardingStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record OrganizationSettingRecord(Guid Id, string Settings, DateTimeOffset? UpdatedAt);
public sealed record OrganizationUsageRecord(string MetricKey, decimal MetricValue, DateOnly PeriodMonth, DateTimeOffset? UpdatedAt);
