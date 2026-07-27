namespace Valora.Application.DTOs;

public sealed record RegisterCompanyRequest(
    string Cnpj,
    string CompanyName,
    string? TradeName,
    string AdministratorName,
    string AdministratorEmail,
    string Password,
    string Phone,
    string Language,
    string TimeZone,
    bool AcceptedTerms,
    bool AcceptedPrivacyPolicy,
    string IdempotencyKey);
