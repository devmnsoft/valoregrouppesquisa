using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class SubscriptionUsageService(IUsageRepository usage, IPlanEntitlementService entitlements)
    : ISubscriptionUsageService
{
    private static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        ["diagnosticsCreated"] = "Diagnósticos criados",
        ["diagnosticsPublished"] = "Diagnósticos publicados",
        ["responsesPerMonth"] = "Respostas recebidas no mês",
        ["users"] = "Usuários ativos",
        ["units"] = "Unidades e grupos",
        ["reports"] = "Relatórios gerados",
        ["certificates"] = "Certificados emitidos",
        ["aiExecutions"] = "Execuções de IA",
        ["exports"] = "Exportações realizadas"
    };

    public async Task<SubscriptionUsageDto> GetCurrentAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;
        await usage.RecalculateAsync(organizationId, now);
        var current = await usage.GetMonthlyAsync(organizationId, now);
        var plan = await entitlements.ResolveAsync(organizationId);
        var metrics = Names.Select(item => new SubscriptionUsageMetric(item.Key, item.Value,
            current.Counters.GetValueOrDefault(item.Key), plan.Limits.GetValueOrDefault(item.Key, -1))).ToArray();
        return new(organizationId, new DateOnly(now.Year, now.Month, 1), metrics);
    }

    public async Task<SubscriptionLimitDecision> CheckAsync(Guid organizationId, string metric, int amount = 1,
        bool isValoraAdmin = false, CancellationToken cancellationToken = default)
    {
        if (isValoraAdmin) return SubscriptionLimitDecision.Granted(metric);
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        var snapshot = await GetCurrentAsync(organizationId, cancellationToken);
        var value = snapshot.Metrics.FirstOrDefault(item => item.Code.Equals(metric, StringComparison.OrdinalIgnoreCase));
        return value is null || value.Unlimited || value.Used + amount <= value.Limit
            ? SubscriptionLimitDecision.Granted(metric)
            : SubscriptionLimitDecision.Denied(metric);
    }
}
