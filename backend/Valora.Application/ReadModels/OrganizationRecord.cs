namespace Valora.Application.ReadModels;

/// <summary>
/// Persistence projection for an organization. PostgreSQL timestamps are kept as
/// <see cref="DateTime"/> here because this type is materialized directly by Dapper/Npgsql.
/// API/domain conversions, when needed, belong outside the persistence projection.
/// </summary>
public sealed class OrganizationRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PublicName { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string Status { get; init; } = "active";
    public string DefaultLanguageCode { get; init; } = "pt-BR";
    public string TimeZone { get; init; } = "America/Belem";
    public string OnboardingStatus { get; init; } = "pending";
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public long Version { get; init; }
    public string? LegalName { get; init; }
    public string? Cnpj { get; init; }
    public string? Segment { get; init; }
    public string? Cnae { get; init; }
    public string? CompanySize { get; init; }
    public int? ApproximateEmployeeCount { get; init; }
    public int? LeadershipCount { get; init; }
    public string? BusinessModel { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PrimaryContactName { get; init; }
    public int MinimumAggregationSize { get; init; } = 5;
}
