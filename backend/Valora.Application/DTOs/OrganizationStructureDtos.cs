namespace Valora.Application.DTOs;

public sealed record UnitResponse(Guid Id, Guid OrganizationId, Guid? LegalEntityId, string Name, string? Code, string? Type, string? Region, string? State, string? City, string Status, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
public sealed record DepartmentResponse(Guid Id, Guid OrganizationId, Guid? UnitId, string Name, string? Code, string? Type, string Status, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
public sealed record UpsertUnitRequest(Guid? LegalEntityId, string Name, string? Code, string? Type, string? Region, string? State, string? City);
public sealed record UpsertDepartmentRequest(Guid? UnitId, string Name, string? Code, string? Type);
