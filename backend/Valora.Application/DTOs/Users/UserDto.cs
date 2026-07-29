namespace Valora.Application.DTOs;

public sealed record UserDto(Guid Id,Guid? OrganizationId,string Name,string Email,string Role,string Status,string? Phone);
