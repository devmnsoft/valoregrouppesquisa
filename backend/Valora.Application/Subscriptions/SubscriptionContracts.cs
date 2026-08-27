namespace Valora.Application.Subscriptions;

public static class SubscriptionFeatures
{
    public const string Diagnostics = "diagnostics";
    public const string Reports = "reports";
    public const string Certificates = "certificates";
    public const string ExecutiveReports = "executive_reports";
    public const string OrganizationalIntelligence = "organizational_intelligence";
    public const string Heatmap = "heatmap";
    public const string Benchmark = "benchmark";
    public const string ActionCenter = "action_center";
    public const string Evolution = "evolution";
    public const string Journey = "journey";
    public const string OneOnOne = "one_on_one";
    public const string DataHub = "datahub";
    public const string PowerBi = "powerbi";
    public const string AnalyticsApi = "analytics_api";
    public const string Webhooks = "webhooks";
    public const string Governance = "governance";
    public const string AdvancedAudit = "advanced_audit";
    public const string MultiUnit = "multi_unit";
    public const string PublicLinks = "public_links";
    public const string Exports = "exports";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [Diagnostics, Reports, Certificates, ExecutiveReports, OrganizationalIntelligence, Heatmap, Benchmark,
            ActionCenter, Evolution, Journey, OneOnOne, DataHub, PowerBi, AnalyticsApi, Webhooks,
            Governance, AdvancedAudit, MultiUnit, PublicLinks, Exports], StringComparer.OrdinalIgnoreCase);
}

public static class SubscriptionMetrics
{
    public const string Diagnostics = "diagnostics"; public const string Respondents = "respondents";
    public const string Users = "users"; public const string StorageMb = "storage_mb";
    public const string Reports = "reports"; public const string Certificates = "certificates";
    public const string ApiCalls = "api_calls";
    public const string PublicLinks = "public_links"; public const string Exports = "exports";
}

public sealed record SubscriptionPlan(Guid Id, string Code, string Name, string Status,
    IReadOnlyDictionary<string, int> Limits, IReadOnlySet<string> Features);
public sealed record OrganizationSubscription(Guid Id, Guid OrganizationId, Guid PlanId, string Status,
    DateTimeOffset StartedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? TrialEndsAt);
public sealed record CurrentSubscription(OrganizationSubscription Subscription, SubscriptionPlan Plan,
    UsageSnapshot Usage, IReadOnlyDictionary<string, int> EffectiveLimits);
public sealed record UsageSnapshot(Guid SubscriptionId, DateOnly PeriodStart, DateOnly PeriodEnd,
    IReadOnlyDictionary<string, int> Counters);
public sealed record FeatureAccessDecision(bool Allowed, string FeatureCode, string Message, string? UpgradeUrl)
{
    public static FeatureAccessDecision Granted(string code) => new(true, code, "Acesso liberado.", null);
    public static FeatureAccessDecision Denied(string code, string message) => new(false, code, message, "/Subscription/Upgrade");
}
public sealed record UsageLimitDecision(bool Allowed, string Metric, int Used, int Limit, string Message, string? UpgradeUrl);
public sealed record UpgradeRequest(Guid Id, Guid OrganizationId, Guid CurrentPlanId, Guid RequestedPlanId,
    Guid RequestedBy, string Reason, string BillingEmail, string Status, DateTimeOffset CreatedAt);

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SubscriptionPlan> GetFreeAsync(CancellationToken ct = default);
}
public interface IOrganizationSubscriptionRepository
{
    Task<OrganizationSubscription?> GetCurrentAsync(Guid organizationId, CancellationToken ct = default);
    Task<OrganizationSubscription> CreateFreeAsync(Guid organizationId, Guid freePlanId, CancellationToken ct = default);
    Task ChangePlanAsync(Guid organizationId, Guid planId, Guid changedBy, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetLimitOverridesAsync(Guid subscriptionId, CancellationToken ct = default);
    Task ApplyLimitOverrideAsync(Guid subscriptionId, string metric, int value, Guid appliedBy, CancellationToken ct = default);
}
public interface IUsageCounterRepository
{
    Task<UsageSnapshot> GetCurrentAsync(Guid organizationId, Guid subscriptionId, CancellationToken ct = default);
    Task RegisterAsync(Guid organizationId, Guid subscriptionId, string metric, int amount, bool blocked,
        string? metadataJson, CancellationToken ct = default);
}
public interface IUpgradeRequestRepository
{
    Task<UpgradeRequest> CreateAsync(UpgradeRequest request, CancellationToken ct = default);
}
