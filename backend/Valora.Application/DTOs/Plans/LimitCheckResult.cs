namespace Valora.Application.DTOs;

public record LimitCheckResult(bool Allowed,string LimitKey,int LimitValue,int CurrentUsage,int RequestedAmount)
{
    public bool Ok => Allowed;
    public string Code => Allowed ? "OK" : "PLAN_LIMIT_REACHED";
    public string Message => Allowed ? "Limite disponível." : "Este recurso está disponível em um plano superior.";
    public bool UpgradeHint => !Allowed;
}
