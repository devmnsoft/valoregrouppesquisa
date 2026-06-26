namespace Valora.Application.DTOs;

public record LimitCheckResult(bool Allowed,string LimitKey,int LimitValue,int CurrentUsage,int RequestedAmount);
