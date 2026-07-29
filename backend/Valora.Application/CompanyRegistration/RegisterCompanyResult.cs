using Valora.Application.Contracts;

namespace Valora.Application.CompanyRegistration;

public sealed record RegisterCompanyResult(Guid OrganizationId, Guid LegalEntityId, Guid UnitId, Guid UserId, bool Replayed);
