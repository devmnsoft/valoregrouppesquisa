namespace Valora.Application.DTOs;

public record UpdateOrganizationRequest(
    string? PublicName,
    string? Phone,
    string? Document = null,
    string? Email = null,
    string? DefaultLanguageCode = null,
    string? TimeZone = null,
    long? ExpectedVersion = null,
    string? LegalName = null,
    string? Cnpj = null,
    string? Segment = null,
    string? Cnae = null,
    string? CompanySize = null,
    int? ApproximateEmployeeCount = null,
    int? LeadershipCount = null,
    string? BusinessModel = null,
    string? Region = null,
    string? City = null,
    string? State = null,
    string? PrimaryContactName = null,
    int? MinimumAggregationSize = null);
