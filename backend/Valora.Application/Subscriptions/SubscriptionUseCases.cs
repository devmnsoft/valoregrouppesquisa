namespace Valora.Application.Subscriptions;

public sealed class GetCurrentSubscriptionUseCase(OrganizationSubscriptionService service)
{ public Task<CurrentSubscription> ExecuteAsync(Guid organizationId, CancellationToken ct = default) => service.GetCurrentAsync(organizationId, ct); }
public sealed class CheckFeatureAccessUseCase(FeatureAccessService service)
{ public Task<FeatureAccessDecision> ExecuteAsync(Guid organizationId, string feature, CancellationToken ct = default) => service.CanAccessAsync(organizationId, feature, ct); }
public sealed class ValidateUsageLimitUseCase(UsageLimitService service)
{ public Task<UsageLimitDecision> ExecuteAsync(Guid organizationId, string metric, int amount = 1, CancellationToken ct = default) => service.ValidateAsync(organizationId, metric, amount, ct); }
public sealed class RegisterUsageEventUseCase(IUsageCounterRepository usage, OrganizationSubscriptionService subscriptions)
{ public async Task ExecuteAsync(Guid organizationId, string metric, int amount = 1, string? metadataJson = null, CancellationToken ct = default, CancellationToken cancellationToken = default) { var current = await subscriptions.GetCurrentAsync(organizationId, ct); await usage.RegisterAsync(organizationId, current.Subscription.Id, metric, amount, false, metadataJson, ct); } }
public sealed class RequestPlanUpgradeUseCase(UpgradeRequestService service)
{ public Task<UpgradeRequest> ExecuteAsync(Guid organizationId, Guid planId, Guid userId, string reason, string email, CancellationToken ct = default) => service.RequestAsync(organizationId, planId, userId, reason, email, ct); }
public sealed class ChangeOrganizationPlanUseCase(IOrganizationSubscriptionRepository subscriptions)
{ public Task ExecuteAsync(Guid organizationId, Guid planId, Guid superAdminId, bool isSuperAdmin, CancellationToken ct = default) => isSuperAdmin ? subscriptions.ChangePlanAsync(organizationId, planId, superAdminId, ct) : throw new UnauthorizedAccessException("Somente o Super Admin pode alterar planos."); }
public sealed class ApplyPlanLimitOverrideUseCase(IOrganizationSubscriptionRepository subscriptions, OrganizationSubscriptionService current)
{ public async Task ExecuteAsync(Guid organizationId, string metric, int value, Guid superAdminId, bool isSuperAdmin, CancellationToken ct = default) { if (!isSuperAdmin) throw new UnauthorizedAccessException("Somente o Super Admin pode aplicar overrides."); var subscription = await current.GetCurrentAsync(organizationId, ct); if (!subscription.Plan.Code.Equals("enterprise", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Overrides são exclusivos do plano Enterprise."); await subscriptions.ApplyLimitOverrideAsync(subscription.Subscription.Id, metric, value, superAdminId, ct); } }
