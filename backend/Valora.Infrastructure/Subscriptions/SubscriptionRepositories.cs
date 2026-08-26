using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Subscriptions;

namespace Valora.Infrastructure.Subscriptions;

public sealed class SubscriptionPlanRepository(IDbConnectionFactory connections) : ISubscriptionPlanRepository
{
    public Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default) => GetAsync(id, null, ct);

    public async Task<SubscriptionPlan> GetFreeAsync(CancellationToken ct = default) =>
        await GetAsync(null, "free", ct) ?? throw new InvalidOperationException("O plano Free ativo não foi configurado.");

    private async Task<SubscriptionPlan?> GetAsync(Guid? id, string? code, CancellationToken ct)
    {
        const string planSql = """
            SELECT id AS Id, code AS Code, name AS Name, status AS Status
            FROM valorapesquisa.subscription_plans
            WHERE deleted_at IS NULL AND ((@Id IS NOT NULL AND id = @Id) OR (@Code IS NOT NULL AND lower(code) = lower(@Code)))
            LIMIT 1;
            """;
        const string limitsSql = """
            SELECT metric, limit_value
            FROM valorapesquisa.subscription_plan_limits
            WHERE plan_id = @PlanId;
            """;
        const string featuresSql = """
            SELECT feature_code
            FROM valorapesquisa.subscription_plan_features
            WHERE plan_id = @PlanId AND enabled = true;
            """;

        using var connection = connections.Create();
        var row = await connection.QuerySingleOrDefaultAsync<PlanRow>(
            new CommandDefinition(planSql, new { Id = id, Code = code }, cancellationToken: ct));
        if (row is null) return null;

        var limitRows = await connection.QueryAsync<LimitRow>(
            new CommandDefinition(limitsSql, new { PlanId = row.Id }, cancellationToken: ct));
        var features = await connection.QueryAsync<string>(
            new CommandDefinition(featuresSql, new { PlanId = row.Id }, cancellationToken: ct));
        return new SubscriptionPlan(row.Id, row.Code, row.Name, row.Status,
            limitRows.ToDictionary(item => item.Metric, item => item.LimitValue ?? -1, StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(features, StringComparer.OrdinalIgnoreCase));
    }

    private sealed record PlanRow(Guid Id, string Code, string Name, string Status);
    private sealed record LimitRow(string Metric, int? LimitValue);
}

public sealed class OrganizationSubscriptionRepository(IDbConnectionFactory connections) : IOrganizationSubscriptionRepository
{
    public async Task<OrganizationSubscription?> GetCurrentAsync(Guid organizationId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id AS Id, organization_id AS OrganizationId, plan_id AS PlanId, status AS Status,
                   started_at AS StartedAt, expires_at AS ExpiresAt, trial_ends_at AS TrialEndsAt
            FROM valorapesquisa.organization_subscriptions
            WHERE organization_id = @OrganizationId AND deleted_at IS NULL
            ORDER BY created_at DESC LIMIT 1;
            """;
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<OrganizationSubscription>(
            new CommandDefinition(sql, new { OrganizationId = organizationId }, cancellationToken: ct));
    }

    public async Task<OrganizationSubscription> CreateFreeAsync(Guid organizationId, Guid freePlanId, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO valorapesquisa.organization_subscriptions (organization_id, plan_id, status)
            VALUES (@OrganizationId, @PlanId, 'active')
            ON CONFLICT (organization_id) WHERE deleted_at IS NULL
            DO UPDATE SET updated_at = now()
            RETURNING id AS Id, organization_id AS OrganizationId, plan_id AS PlanId, status AS Status,
                      started_at AS StartedAt, expires_at AS ExpiresAt, trial_ends_at AS TrialEndsAt;
            """;
        using var connection = connections.Create();
        return await connection.QuerySingleAsync<OrganizationSubscription>(
            new CommandDefinition(sql, new { OrganizationId = organizationId, PlanId = freePlanId }, cancellationToken: ct));
    }

    public async Task ChangePlanAsync(Guid organizationId, Guid planId, Guid changedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE valorapesquisa.organization_subscriptions
            SET plan_id = @PlanId, status = 'active', updated_at = now(),
                metadata_json = metadata_json || jsonb_build_object('changed_by', CAST(@ChangedBy AS text), 'changed_at', now())
            WHERE organization_id = @OrganizationId AND deleted_at IS NULL;
            """;
        using var connection = connections.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql,
            new { OrganizationId = organizationId, PlanId = planId, ChangedBy = changedBy }, cancellationToken: ct));
        if (affected == 0) throw new InvalidOperationException("A organização não possui assinatura ativa para alteração.");
    }

    public async Task<IReadOnlyDictionary<string, int>> GetLimitOverridesAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        const string sql = "SELECT metric, limit_value FROM valorapesquisa.plan_limit_overrides WHERE subscription_id = @SubscriptionId AND deleted_at IS NULL;";
        using var connection = connections.Create();
        var rows = await connection.QueryAsync<OverrideRow>(new CommandDefinition(sql, new { SubscriptionId = subscriptionId }, cancellationToken: ct));
        return rows.ToDictionary(item => item.Metric, item => item.LimitValue, StringComparer.OrdinalIgnoreCase);
    }

    public async Task ApplyLimitOverrideAsync(Guid subscriptionId, string metric, int value, Guid appliedBy, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO valorapesquisa.plan_limit_overrides (subscription_id, metric, limit_value, applied_by)
            VALUES (@SubscriptionId, @Metric, @Value, @AppliedBy)
            ON CONFLICT (subscription_id, metric) WHERE deleted_at IS NULL
            DO UPDATE SET limit_value = EXCLUDED.limit_value, applied_by = EXCLUDED.applied_by, updated_at = now();
            """;
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { SubscriptionId = subscriptionId, Metric = metric, Value = value, AppliedBy = appliedBy }, cancellationToken: ct));
    }

    private sealed record OverrideRow(string Metric, int LimitValue);
}

public sealed class UsageCounterRepository(IDbConnectionFactory connections) : IUsageCounterRepository
{
    public async Task<UsageSnapshot> GetCurrentAsync(Guid organizationId, Guid subscriptionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT subscription_id AS SubscriptionId, period_start AS PeriodStart, period_end AS PeriodEnd,
                   diagnostics_used AS Diagnostics, respondents_used AS Respondents, users_used AS Users,
                   storage_mb_used AS StorageMb, reports_generated AS Reports, certificates_generated AS Certificates,
                   api_calls_used AS ApiCalls
            FROM valorapesquisa.subscription_usage_counters
            WHERE organization_id = @OrganizationId AND subscription_id = @SubscriptionId
              AND CURRENT_DATE BETWEEN period_start AND period_end
            ORDER BY period_start DESC LIMIT 1;
            """;
        using var connection = connections.Create();
        var row = await connection.QuerySingleOrDefaultAsync<UsageRow>(new CommandDefinition(sql,
            new { OrganizationId = organizationId, SubscriptionId = subscriptionId }, cancellationToken: ct));
        var start = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return row is null
            ? new UsageSnapshot(subscriptionId, start, start.AddMonths(1).AddDays(-1), EmptyCounters())
            : new UsageSnapshot(row.SubscriptionId, row.PeriodStart, row.PeriodEnd, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [SubscriptionMetrics.Diagnostics] = row.Diagnostics, [SubscriptionMetrics.Respondents] = row.Respondents,
                [SubscriptionMetrics.Users] = row.Users, [SubscriptionMetrics.StorageMb] = row.StorageMb,
                [SubscriptionMetrics.Reports] = row.Reports, [SubscriptionMetrics.Certificates] = row.Certificates,
                [SubscriptionMetrics.ApiCalls] = row.ApiCalls
            });
    }

    public async Task RegisterAsync(Guid organizationId, Guid subscriptionId, string metric, int amount, bool blocked,
        string? metadataJson, CancellationToken ct = default)
    {
        var column = metric.ToLowerInvariant() switch
        {
            SubscriptionMetrics.Diagnostics => "diagnostics_used", SubscriptionMetrics.Respondents => "respondents_used",
            SubscriptionMetrics.Users => "users_used", SubscriptionMetrics.StorageMb => "storage_mb_used",
            SubscriptionMetrics.Reports => "reports_generated", SubscriptionMetrics.Certificates => "certificates_generated",
            SubscriptionMetrics.ApiCalls => "api_calls_used", _ => null
        };
        using var connection = connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        const string eventSql = """
            INSERT INTO valorapesquisa.subscription_usage_events
                (organization_id, subscription_id, metric, amount, blocked, metadata_json)
            VALUES (@OrganizationId, @SubscriptionId, @Metric, @Amount, @Blocked, CAST(COALESCE(@MetadataJson, '{}') AS jsonb));
            """;
        await connection.ExecuteAsync(new CommandDefinition(eventSql,
            new { OrganizationId = organizationId, SubscriptionId = subscriptionId, Metric = metric, Amount = amount, Blocked = blocked, MetadataJson = metadataJson }, transaction, cancellationToken: ct));
        if (!blocked && column is not null)
        {
            var counterSql = $"""
                INSERT INTO valorapesquisa.subscription_usage_counters
                    (organization_id, subscription_id, period_start, period_end, {column})
                VALUES (@OrganizationId, @SubscriptionId, date_trunc('month', CURRENT_DATE)::date,
                        (date_trunc('month', CURRENT_DATE) + interval '1 month - 1 day')::date, @Amount)
                ON CONFLICT (subscription_id, period_start)
                DO UPDATE SET {column} = valorapesquisa.subscription_usage_counters.{column} + EXCLUDED.{column}, updated_at = now();
                """;
            await connection.ExecuteAsync(new CommandDefinition(counterSql,
                new { OrganizationId = organizationId, SubscriptionId = subscriptionId, Amount = amount }, transaction, cancellationToken: ct));
        }
        transaction.Commit();
    }

    private static IReadOnlyDictionary<string, int> EmptyCounters() =>
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private sealed record UsageRow(Guid SubscriptionId, DateOnly PeriodStart, DateOnly PeriodEnd, int Diagnostics,
        int Respondents, int Users, int StorageMb, int Reports, int Certificates, int ApiCalls);
}

public sealed class UpgradeRequestRepository(IDbConnectionFactory connections) : IUpgradeRequestRepository
{
    public async Task<UpgradeRequest> CreateAsync(UpgradeRequest request, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO valorapesquisa.subscription_upgrade_requests
                (id, organization_id, current_plan_id, requested_plan_id, requested_by, reason, billing_email, status, created_at)
            VALUES (@Id, @OrganizationId, @CurrentPlanId, @RequestedPlanId, @RequestedBy, @Reason, @BillingEmail, @Status, @CreatedAt)
            RETURNING id AS Id, organization_id AS OrganizationId, current_plan_id AS CurrentPlanId,
                      requested_plan_id AS RequestedPlanId, requested_by AS RequestedBy, reason AS Reason,
                      billing_email AS BillingEmail, status AS Status, created_at AS CreatedAt;
            """;
        using var connection = connections.Create();
        return await connection.QuerySingleAsync<UpgradeRequest>(new CommandDefinition(sql, request, cancellationToken: ct));
    }
}
