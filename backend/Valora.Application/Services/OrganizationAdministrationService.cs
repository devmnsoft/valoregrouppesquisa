using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;
using Valora.Application.ReadModels;

namespace Valora.Application.Services;

public sealed class OrganizationAdministrationService(IOrganizationRepository organizations) : IOrganizationAdministrationService
{
    public async Task<OrganizationRecord> GetCurrentAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await organizations.GetAsync(organizationId, cancellationToken)
        ?? throw new NotFoundAppException("Organização não encontrada.");

    public async Task<OrganizationRecord> UpdateCurrentAsync(Guid organizationId, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ExpectedVersion is null or < 1)
            throw new ValidationAppException("expectedVersion é obrigatório.");

        var version = await organizations.UpdateCurrentAsync(organizationId, request, cancellationToken);
        if (version is null)
        {
            if (await organizations.GetAsync(organizationId, cancellationToken) is null)
                throw new NotFoundAppException("Organização não encontrada.");
            throw new ConcurrencyConflictException("A organização foi atualizada por outra sessão.");
        }

        return await GetCurrentAsync(organizationId, cancellationToken);
    }
}
