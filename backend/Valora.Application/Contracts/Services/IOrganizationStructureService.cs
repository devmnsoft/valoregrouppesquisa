using Valora.Application.DTOs;

namespace Valora.Application.Contracts;

public interface IOrganizationStructureService
{
    Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(Guid organizationId, string? status = null, CancellationToken cancellationToken = default);
    Task<UnitResponse> CreateUnitAsync(Guid organizationId, UpsertUnitRequest request, CancellationToken cancellationToken = default);
    Task<UnitResponse> UpdateUnitAsync(Guid organizationId, Guid id, UpsertUnitRequest request, CancellationToken cancellationToken = default);
    Task<UnitResponse> SetUnitStatusAsync(Guid organizationId, Guid id, bool active, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentResponse>> ListDepartmentsAsync(Guid organizationId, Guid? unitId = null, string? status = null, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> CreateDepartmentAsync(Guid organizationId, UpsertDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> UpdateDepartmentAsync(Guid organizationId, Guid id, UpsertDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> SetDepartmentStatusAsync(Guid organizationId, Guid id, bool active, CancellationToken cancellationToken = default);
}
