namespace Valora.Application.DTOs;

public sealed record SubscriptionUsageMetric(string Code, string Name, int Used, int Limit)
{
    public bool Unlimited => Limit < 0;
    public decimal PercentUsed => Unlimited || Limit == 0 ? (Limit == 0 && Used > 0 ? 100 : 0) : Math.Min(100, Math.Round(Used * 100m / Limit, 1));
    public bool LimitReached => !Unlimited && Used >= Limit;
}

public sealed record SubscriptionUsageDto(
    Guid OrganizationId,
    DateOnly Competence,
    IReadOnlyList<SubscriptionUsageMetric> Metrics);

public sealed record SubscriptionLimitDecision(bool Allowed, string Code, string Message, string? UpgradeUrl)
{
    public static SubscriptionLimitDecision Granted(string code) => new(true, code, "Ação permitida pelo plano atual.", null);
    public static SubscriptionLimitDecision Denied(string code) => new(false, code,
        "O limite do seu plano foi atingido. Seus dados continuam seguros; faça upgrade para realizar uma nova ação.",
        "/Organization/Upgrade");
}
