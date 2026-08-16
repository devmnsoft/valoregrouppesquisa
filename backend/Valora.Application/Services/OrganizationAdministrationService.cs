using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;
using Valora.Application.ReadModels;

namespace Valora.Application.Services;

public sealed class OrganizationAdministrationService(IOrganizationRepository organizations, IAuditRepository audit) : IOrganizationAdministrationService
{
    public async Task<OrganizationRecord> GetCurrentAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await organizations.GetAsync(organizationId, cancellationToken)
        ?? throw new NotFoundAppException("Organização não encontrada.");

    public async Task<OrganizationRecord> UpdateCurrentAsync(Guid organizationId, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ExpectedVersion is null or < 1)
            throw new ValidationAppException("expectedVersion é obrigatório.");
        var cnpj = new string((request.Cnpj ?? string.Empty).Where(char.IsDigit).ToArray());
        if (request.Cnpj is not null && cnpj.Length != 14)
            throw new ValidationAppException("CNPJ deve conter 14 dígitos.");
        if (request.MinimumAggregationSize is < 3 or > 100)
            throw new ValidationAppException("A agregação mínima deve estar entre 3 e 100 participantes.");
        request = request with { Cnpj = request.Cnpj is null ? null : cnpj, State = request.State?.Trim().ToUpperInvariant() };

        var version = await organizations.UpdateCurrentAsync(organizationId, request, cancellationToken);
        if (version is null)
        {
            if (await organizations.GetAsync(organizationId, cancellationToken) is null)
                throw new NotFoundAppException("Organização não encontrada.");
            throw new ConcurrencyConflictException("A organização foi atualizada por outra sessão.");
        }

        var updated = await GetCurrentAsync(organizationId, cancellationToken);
        await audit.AddAsync(new AuditEntry(organizationId, null, "organization.profile.updated", "organization", organizationId.ToString(), "Dados organizacionais e regras de privacidade atualizados."));
        return updated;
    }
}
