using Valora.Application.DTOs;

namespace Valora.Application.Contracts;

public interface IOrganizationStructureRepository
{
    Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(Guid organizationId, string? status = null, CancellationToken cancellationToken = default);
    Task<UnitResponse?> GetUnitAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
    Task<UnitResponse> CreateUnitAsync(Guid organizationId, UpsertUnitRequest request, CancellationToken cancellationToken = default);
    Task<UnitResponse?> UpdateUnitAsync(Guid organizationId, Guid id, UpsertUnitRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetUnitStatusAsync(Guid organizationId, Guid id, string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentResponse>> ListDepartmentsAsync(Guid organizationId, Guid? unitId = null, string? status = null, CancellationToken cancellationToken = default);
    Task<DepartmentResponse?> GetDepartmentAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> CreateDepartmentAsync(Guid organizationId, UpsertDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentResponse?> UpdateDepartmentAsync(Guid organizationId, Guid id, UpsertDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetDepartmentStatusAsync(Guid organizationId, Guid id, string status, CancellationToken cancellationToken = default);
}
