namespace Valora.Application.Subscriptions;

public sealed class SubscriptionPlanService(ISubscriptionPlanRepository plans)
{
    public Task<SubscriptionPlan> GetFreeAsync(CancellationToken ct = default) => plans.GetFreeAsync(ct);
}

public sealed class OrganizationSubscriptionService(ISubscriptionPlanRepository plans,
    IOrganizationSubscriptionRepository subscriptions, IUsageCounterRepository usage)
{
    public async Task<CurrentSubscription> GetCurrentAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("A organização é obrigatória.", nameof(organizationId));
        var subscription = await subscriptions.GetCurrentAsync(organizationId, ct);
        if (subscription is null)
        {
            var free = await plans.GetFreeAsync(ct);
            subscription = await subscriptions.CreateFreeAsync(organizationId, free.Id, ct);
        }
        var plan = await plans.GetByIdAsync(subscription.PlanId, ct) ?? await plans.GetFreeAsync(ct);
        var snapshot = await usage.GetCurrentAsync(organizationId, subscription.Id, ct);
        var limits = new Dictionary<string, int>(plan.Limits, StringComparer.OrdinalIgnoreCase);
        if (plan.Code.Equals("enterprise", StringComparison.OrdinalIgnoreCase))
            foreach (var item in await subscriptions.GetLimitOverridesAsync(subscription.Id, ct)) limits[item.Key] = item.Value;
        return new(subscription, plan, snapshot, limits);
    }
}

public sealed class FeatureAccessService(OrganizationSubscriptionService subscriptions, IUsageCounterRepository usage)
{
    public async Task<FeatureAccessDecision> CanAccessAsync(Guid organizationId, string featureCode, CancellationToken ct = default)
    {
        if (!SubscriptionFeatures.All.Contains(featureCode)) throw new ArgumentException("Código de funcionalidade inválido.", nameof(featureCode));
        var current = await subscriptions.GetCurrentAsync(organizationId, ct);
        var now = DateTimeOffset.UtcNow;
        var active = current.Plan.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
            && current.Subscription.Status is "active" or "trialing"
            && (current.Subscription.ExpiresAt is not { } expires || expires > now);
        active = active && (current.Subscription.Status != "trialing" || current.Subscription.TrialEndsAt is { } trial && trial > now);
        if (active && current.Plan.Features.Contains(featureCode)) return FeatureAccessDecision.Granted(featureCode);
        var message = !active ? "Sua assinatura não está ativa. Escolha um plano para continuar."
            : $"O recurso {featureCode} não está incluído no plano {current.Plan.Name}. Faça upgrade para liberar.";
        await usage.RegisterAsync(organizationId, current.Subscription.Id, $"blocked:{featureCode}", 1, true, null, ct);
        return FeatureAccessDecision.Denied(featureCode, message);
    }
}

public sealed class UsageLimitService(OrganizationSubscriptionService subscriptions, IUsageCounterRepository usage)
{
    public async Task<UsageLimitDecision> ValidateAsync(Guid organizationId, string metric, int amount = 1, CancellationToken ct = default)
    {
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        var current = await subscriptions.GetCurrentAsync(organizationId, ct);
        var used = current.Usage.Counters.GetValueOrDefault(metric);
        var limit = current.EffectiveLimits.GetValueOrDefault(metric, 0);
        var allowed = limit < 0 || used + amount <= limit;
        if (!allowed) await usage.RegisterAsync(organizationId, current.Subscription.Id, $"limit:{metric}", amount, true, null, ct);
        return new(allowed, metric, used, limit, allowed ? "Limite disponível."
            : "O limite do seu plano foi atingido. Solicite um upgrade para continuar.", allowed ? null : "/Subscription/Upgrade");
    }
}

public sealed class UpgradeRequestService(OrganizationSubscriptionService subscriptions, IUpgradeRequestRepository requests)
{
    public async Task<UpgradeRequest> RequestAsync(Guid organizationId, Guid requestedPlanId, Guid requestedBy,
        string reason, string billingEmail, CancellationToken ct = default)
    {
        if (requestedPlanId == Guid.Empty || requestedBy == Guid.Empty) throw new ArgumentException("Plano e solicitante são obrigatórios.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) throw new ArgumentException("Informe um motivo com até 1000 caracteres.");
        if (string.IsNullOrWhiteSpace(billingEmail) || !billingEmail.Contains('@')) throw new ArgumentException("Informe um e-mail financeiro válido.");
        var current = await subscriptions.GetCurrentAsync(organizationId, ct);
        if (current.Plan.Id == requestedPlanId) throw new InvalidOperationException("A organização já utiliza o plano solicitado.");
        return await requests.CreateAsync(new(Guid.NewGuid(), organizationId, current.Plan.Id, requestedPlanId,
            requestedBy, reason.Trim(), billingEmail.Trim(), "pending", DateTimeOffset.UtcNow), ct);
    }
}

public sealed class BillingContactService { }
