using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;

namespace Valora.Application.Services;

public sealed class OrganizationStructureService(IOrganizationStructureRepository repository, IPlanEntitlementService entitlements) : IOrganizationStructureService
{
    private const string LimitMessage = "Seu plano atual não permite esta ação. Para continuar, faça upgrade ou libere espaço removendo registros antigos.";

    public Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(Guid organizationId, string? status = null, CancellationToken cancellationToken = default) => repository.ListUnitsAsync(organizationId, status);
    public async Task<UnitResponse> CreateUnitAsync(Guid organizationId, UpsertUnitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Nome da unidade é obrigatório.");
        var check = await entitlements.CheckLimitAsync(organizationId, "units", 1);
        if (!check.Allowed) throw new BusinessRuleAppException(LimitMessage);
        return await repository.CreateUnitAsync(organizationId, request);
    }
    public async Task<UnitResponse> UpdateUnitAsync(Guid organizationId, Guid id, UpsertUnitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Nome da unidade é obrigatório.");
        return await repository.UpdateUnitAsync(organizationId, id, request, cancellationToken) ?? throw new NotFoundAppException("Unidade não encontrada.");
    }
    public async Task<UnitResponse> SetUnitStatusAsync(Guid organizationId, Guid id, bool active, CancellationToken cancellationToken = default)
    {
        if (!await repository.SetUnitStatusAsync(organizationId, id, active ? "active" : "inactive", cancellationToken)) throw new NotFoundAppException("Unidade não encontrada.");
        return await repository.GetUnitAsync(organizationId, id, cancellationToken) ?? throw new NotFoundAppException("Unidade não encontrada.");
    }
    public Task<IReadOnlyList<DepartmentResponse>> ListDepartmentsAsync(Guid organizationId, Guid? unitId = null, string? status = null, CancellationToken cancellationToken = default) => repository.ListDepartmentsAsync(organizationId, unitId, status);
    public async Task<DepartmentResponse> CreateDepartmentAsync(Guid organizationId, UpsertDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Nome do setor é obrigatório.");
        var check = await entitlements.CheckLimitAsync(organizationId, "departments", 1);
        if (!check.Allowed) throw new BusinessRuleAppException(LimitMessage);
        return await repository.CreateDepartmentAsync(organizationId, request);
    }
    public async Task<DepartmentResponse> UpdateDepartmentAsync(Guid organizationId, Guid id, UpsertDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Nome do setor é obrigatório.");
        return await repository.UpdateDepartmentAsync(organizationId, id, request, cancellationToken) ?? throw new NotFoundAppException("Setor não encontrado.");
    }
    public async Task<DepartmentResponse> SetDepartmentStatusAsync(Guid organizationId, Guid id, bool active, CancellationToken cancellationToken = default)
    {
        if (!await repository.SetDepartmentStatusAsync(organizationId, id, active ? "active" : "inactive", cancellationToken)) throw new NotFoundAppException("Setor não encontrado.");
        return await repository.GetDepartmentAsync(organizationId, id, cancellationToken) ?? throw new NotFoundAppException("Setor não encontrado.");
    }
    private static void ValidateName(string? name, string message) { if (string.IsNullOrWhiteSpace(name) || name.Length > 180) throw new ValidationAppException(message); }
}
