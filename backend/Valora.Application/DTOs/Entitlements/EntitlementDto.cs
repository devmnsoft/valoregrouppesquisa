namespace Valora.Application.DTOs;

public sealed record EntitlementDto(Guid OrganizationId,string PlanCode,IReadOnlyList<string> EnabledModules,IReadOnlyDictionary<string,int> Limits);
