namespace Valora.Application.DTOs;

public record UpdateOrganizationRequest(
    string? PublicName,
    string? Phone,
    string? Document = null,
    string? Email = null,
    string? DefaultLanguageCode = null,
    string? TimeZone = null,
    long? ExpectedVersion = null);
