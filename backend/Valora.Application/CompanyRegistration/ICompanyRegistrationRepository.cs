using Valora.Application.Contracts;

namespace Valora.Application.CompanyRegistration;

public interface ICompanyRegistrationRepository
{
    Task<RegisterCompanyResult> RegisterAsync(IUnitOfWork unitOfWork, RegisterCompanyCommand command);
}
