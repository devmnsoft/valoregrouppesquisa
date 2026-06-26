namespace Valora.Application.DTOs;

public record PlanEntitlements(string PlanId,Dictionary<string,int> Limits,Dictionary<string,string> Capabilities);
