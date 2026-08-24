using Valora.Application.Contracts;

namespace Valora.Application.CompanyRegistration;

public sealed record RegisterCompanyCommand(string IdempotencyKey, string RequestHash, string Cnpj, string CompanyName,
    string? TradeName, string AdministratorName, string AdministratorEmail, string PasswordHash, string Phone,
    string Language, string TimeZone, string ConsentIpHash, string PlanCode, string SubscriptionStatus,
    DateTimeOffset? TrialEndsAt, string? RoleTitle);
