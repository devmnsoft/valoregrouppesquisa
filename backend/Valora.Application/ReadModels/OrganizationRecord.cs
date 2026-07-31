namespace Valora.Application.ReadModels;

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
    DateTimeOffset? UpdatedAt,
    long Version);
