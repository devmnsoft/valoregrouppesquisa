using Valora.Application.Contracts;

namespace Valora.Application.CompanyRegistration;

public sealed record RegisterCompanyCommand(string IdempotencyKey, string RequestHash, string Cnpj, string CompanyName,
    string? TradeName, string AdministratorName, string AdministratorEmail, string PasswordHash, string Phone,
    string Language, string TimeZone, string ConsentIpHash);
public sealed record RegisterCompanyResult(Guid OrganizationId, Guid LegalEntityId, Guid UnitId, Guid UserId, bool Replayed);

public interface ICompanyRegistrationRepository
{
    Task<RegisterCompanyResult> RegisterAsync(IUnitOfWork unitOfWork, RegisterCompanyCommand command);
}
