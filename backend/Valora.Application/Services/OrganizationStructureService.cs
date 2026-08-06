using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;

namespace Valora.Application.Services;

public sealed class OrganizationStructureService(IOrganizationStructureRepository repository, IPlanEntitlementService entitlements, IAuditRepository audit) : IOrganizationStructureService
{
    private const string LimitMessage = "Seu plano atual não permite esta ação. Para continuar, faça upgrade ou libere espaço removendo registros antigos.";

    public Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(Guid organizationId, string? status = null, CancellationToken cancellationToken = default) => repository.ListUnitsAsync(organizationId, status);
    public async Task<UnitResponse> CreateUnitAsync(Guid organizationId, UpsertUnitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Nome da unidade é obrigatório.");
        var check = await entitlements.CheckLimitAsync(organizationId, "units", 1);
        if (!check.Allowed) throw new BusinessRuleAppException(LimitMessage);
        var unit = await repository.CreateUnitAsync(organizationId, request);
        await AuditAsync(organizationId, "unit.created", "unit", unit.Id);
        return unit;
    }
    public async Task<UnitResponse> UpdateUnitAsync(Guid organizationId, Guid id, UpsertUnitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Nome da unidade é obrigatório.");
        var unit = await repository.UpdateUnitAsync(organizationId, id, request, cancellationToken) ?? throw new NotFoundAppException("Unidade não encontrada.");
        await AuditAsync(organizationId, "unit.updated", "unit", id);
        return unit;
    }
    public async Task<UnitResponse> SetUnitStatusAsync(Guid organizationId, Guid id, bool active, CancellationToken cancellationToken = default)
    {
        if (!await repository.SetUnitStatusAsync(organizationId, id, active ? "active" : "inactive", cancellationToken)) throw new NotFoundAppException("Unidade não encontrada.");
        await AuditAsync(organizationId, active ? "unit.reactivated" : "unit.deactivated", "unit", id);
        return await repository.GetUnitAsync(organizationId, id, cancellationToken) ?? throw new NotFoundAppException("Unidade não encontrada.");
    }
    public Task<IReadOnlyList<DepartmentResponse>> ListDepartmentsAsync(Guid organizationId, Guid? unitId = null, string? status = null, CancellationToken cancellationToken = default) => repository.ListDepartmentsAsync(organizationId, unitId, status);
    public async Task<DepartmentResponse> CreateDepartmentAsync(Guid organizationId, UpsertDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Nome do setor é obrigatório.");
        await ValidateUnitScopeAsync(organizationId, request.UnitId, cancellationToken);
        var check = await entitlements.CheckLimitAsync(organizationId, "departments", 1);
        if (!check.Allowed) throw new BusinessRuleAppException(LimitMessage);
        var department = await repository.CreateDepartmentAsync(organizationId, request);
        await AuditAsync(organizationId, "department.created", "department", department.Id);
        return department;
    }
    public async Task<DepartmentResponse> UpdateDepartmentAsync(Guid organizationId, Guid id, UpsertDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Nome do setor é obrigatório.");
        await ValidateUnitScopeAsync(organizationId, request.UnitId, cancellationToken);
        var department = await repository.UpdateDepartmentAsync(organizationId, id, request, cancellationToken) ?? throw new NotFoundAppException("Setor não encontrado.");
        await AuditAsync(organizationId, "department.updated", "department", id);
        return department;
    }
    public async Task<DepartmentResponse> SetDepartmentStatusAsync(Guid organizationId, Guid id, bool active, CancellationToken cancellationToken = default)
    {
        if (!await repository.SetDepartmentStatusAsync(organizationId, id, active ? "active" : "inactive", cancellationToken)) throw new NotFoundAppException("Setor não encontrado.");
        await AuditAsync(organizationId, active ? "department.reactivated" : "department.deactivated", "department", id);
        return await repository.GetDepartmentAsync(organizationId, id, cancellationToken) ?? throw new NotFoundAppException("Setor não encontrado.");
    }
    private static void ValidateName(string? name, string message) { if (string.IsNullOrWhiteSpace(name) || name.Length > 180) throw new ValidationAppException(message); }
    private async Task ValidateUnitScopeAsync(Guid organizationId, Guid? unitId, CancellationToken cancellationToken)
    {
        if (unitId.HasValue && await repository.GetUnitAsync(organizationId, unitId.Value, cancellationToken) is null)
            throw new ValidationAppException("A unidade selecionada não pertence à sua empresa.");
    }
    private Task AuditAsync(Guid organizationId, string action, string entity, Guid id) =>
        audit.AddAsync(new AuditEntry(organizationId, null, action, entity, id.ToString(), "Estrutura organizacional alterada."));
}
