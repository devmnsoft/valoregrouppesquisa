using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
public sealed class OrganizationStructureController(IOrganizationStructureService service) : ControllerBase
{
    [HttpGet("/api/v1/units")]
    public async Task<IActionResult> Units([FromQuery] string? status, CancellationToken cancellationToken) => Ok(await service.ListUnitsAsync(CurrentOrganizationId(), status, cancellationToken));
    [HttpPost("/api/v1/units")]
    public async Task<IActionResult> CreateUnit(UpsertUnitRequest request, CancellationToken cancellationToken) => Ok(await service.CreateUnitAsync(CurrentOrganizationId(), request, cancellationToken));
    [HttpPut("/api/v1/units/{id:guid}")]
    public async Task<IActionResult> UpdateUnit(Guid id, UpsertUnitRequest request, CancellationToken cancellationToken) => Ok(await service.UpdateUnitAsync(CurrentOrganizationId(), id, request, cancellationToken));
    [HttpPost("/api/v1/units/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUnit(Guid id, CancellationToken cancellationToken) => Ok(await service.SetUnitStatusAsync(CurrentOrganizationId(), id, false, cancellationToken));
    [HttpPost("/api/v1/units/{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUnit(Guid id, CancellationToken cancellationToken) => Ok(await service.SetUnitStatusAsync(CurrentOrganizationId(), id, true, cancellationToken));

    [HttpGet("/api/v1/departments")]
    public async Task<IActionResult> Departments([FromQuery] Guid? unitId, [FromQuery] string? status, CancellationToken cancellationToken) => Ok(await service.ListDepartmentsAsync(CurrentOrganizationId(), unitId, status, cancellationToken));
    [HttpPost("/api/v1/departments")]
    public async Task<IActionResult> CreateDepartment(UpsertDepartmentRequest request, CancellationToken cancellationToken) => Ok(await service.CreateDepartmentAsync(CurrentOrganizationId(), request, cancellationToken));
    [HttpPut("/api/v1/departments/{id:guid}")]
    public async Task<IActionResult> UpdateDepartment(Guid id, UpsertDepartmentRequest request, CancellationToken cancellationToken) => Ok(await service.UpdateDepartmentAsync(CurrentOrganizationId(), id, request, cancellationToken));
    [HttpPost("/api/v1/departments/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateDepartment(Guid id, CancellationToken cancellationToken) => Ok(await service.SetDepartmentStatusAsync(CurrentOrganizationId(), id, false, cancellationToken));
    [HttpPost("/api/v1/departments/{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateDepartment(Guid id, CancellationToken cancellationToken) => Ok(await service.SetDepartmentStatusAsync(CurrentOrganizationId(), id, true, cancellationToken));

    private Guid CurrentOrganizationId() => Guid.Parse(User.FindFirstValue("organization_id")!);
}
