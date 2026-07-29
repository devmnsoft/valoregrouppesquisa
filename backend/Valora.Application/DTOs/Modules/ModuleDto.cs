namespace Valora.Application.DTOs;

public sealed record ModuleDto(Guid Id,string Code,string Name,string? Description,string Category,string Status,int DisplayOrder,string? MinimumPlanCode);
