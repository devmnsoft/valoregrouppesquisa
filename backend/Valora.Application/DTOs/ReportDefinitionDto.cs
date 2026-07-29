namespace Valora.Application.DTOs;

public sealed record ReportDefinitionDto(Guid Id,string Code,string Name,string? Description,string ReportType,string RequiredModuleCode,string? MinimumPlanCode,string Status);
