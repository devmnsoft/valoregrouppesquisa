using Valora.Application.DTOs;
using Valora.Application.ReadModels;

namespace Valora.Application.Contracts;

public interface IOrganizationAdministrationService
{
    Task<OrganizationRecord> GetCurrentAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationRecord> UpdateCurrentAsync(Guid organizationId, UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
}
