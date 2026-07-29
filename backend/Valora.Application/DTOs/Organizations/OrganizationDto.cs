namespace Valora.Application.DTOs;

public sealed record OrganizationDto(Guid Id,string Name,string? PublicName,string? Slug,string? Email,string Status,string? PlanCode);
