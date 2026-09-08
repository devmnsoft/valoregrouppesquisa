using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ModularSaas;

namespace Valora.Infrastructure.ModularSaas;

public sealed class CommercialSaasRepository(IDbConnectionFactory connections) : ICommercialSaasRepository
{
    public async Task<IReadOnlyList<CommercialModule>> ListModulesAsync(Guid? clientId, CancellationToken cancellationToken)
    {
        using var connection = connections.Create();
        const string sql = """
            SELECT m.id AS Id, m.code AS Code, m.commercial_name AS Name, m.description AS Description,
                   m.base_price AS BasePrice, m.status AS Status, m.icon AS Icon, m.main_route AS MainRoute,
                   m.menu_category AS MenuCategory, m.requires_contract AS RequiresContract,
                   m.access_module_code AS AccessModuleCode, m.display_order AS DisplayOrder,
                   COALESCE(csm.status, CASE WHEN m.requires_contract THEN 'not_contracted' ELSE 'included' END) AS ContractStatus,
                   (NOT m.requires_contract OR csm.status IN ('active','read_only','suspended','expired')) AS IsContracted,
                   (csm.status IN ('read_only','suspended','expired')) AS IsReadOnly,
                   (SELECT count(*)::int FROM valorapesquisa.saas_module_features f
                     WHERE f.module_id=m.id AND f.status='active' AND f.deleted_at IS NULL) AS FeatureCount
            FROM valorapesquisa.saas_modules m
            LEFT JOIN LATERAL (
                SELECT cs.id, cs.status FROM valorapesquisa.client_subscriptions cs
                 WHERE cs.client_id=CAST(@ClientId AS uuid) AND cs.deleted_at IS NULL
                 ORDER BY cs.created_at DESC LIMIT 1
            ) cs ON CAST(@ClientId AS uuid) IS NOT NULL
            LEFT JOIN valorapesquisa.client_subscription_modules csm
              ON csm.subscription_id=cs.id AND csm.client_id=CAST(@ClientId AS uuid) AND csm.module_id=m.id
            WHERE m.deleted_at IS NULL AND m.status='active'
            ORDER BY m.menu_category,m.display_order,m.commercial_name;
            """;
        var command = new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<CommercialModule>(command)).AsList();
    }

    public async Task<IReadOnlyList<CommercialPlan>> ListPlansAsync(CancellationToken cancellationToken)
    {
        using var connection = connections.Create();
        const string sql = """
            SELECT p.id AS Id,p.code AS Code,p.name AS Name,p.description AS Description,
                   p.monthly_price AS MonthlyPrice,p.annual_price AS AnnualPrice,
                   (SELECT count(*)::int FROM valorapesquisa.saas_plan_modules pm
                     WHERE pm.plan_id=p.id AND pm.included) AS ModuleCount,
                   p.display_order AS DisplayOrder
              FROM valorapesquisa.saas_plans p
             WHERE p.status='active' AND p.is_public AND p.deleted_at IS NULL
             ORDER BY p.display_order,p.name;
            """;
        return (await connection.QueryAsync<CommercialPlan>(new CommandDefinition(sql,
            cancellationToken: cancellationToken))).AsList();
    }

    public async Task<ModuleAccessDecision> EvaluateAccessAsync(Guid clientId, string moduleCode, bool writeOperation,
        CancellationToken cancellationToken)
    {
        using var connection = connections.Create();
        const string sql = """
            SELECT m.requires_contract AS RequiresContract,m.status AS ModuleStatus,
                   cs.status AS SubscriptionStatus,cs.ends_at AS SubscriptionEndsAt,csm.status AS ContractStatus
              FROM valorapesquisa.saas_modules m
              LEFT JOIN LATERAL (
                  SELECT id,status,ends_at FROM valorapesquisa.client_subscriptions
                   WHERE client_id=@ClientId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1
              ) cs ON true
              LEFT JOIN valorapesquisa.client_subscription_modules csm
                ON csm.subscription_id=cs.id AND csm.client_id=@ClientId AND csm.module_id=m.id
             WHERE m.code=@ModuleCode AND m.deleted_at IS NULL;
            """;
        var row = await connection.QuerySingleOrDefaultAsync<ModuleAccessRow>(new CommandDefinition(sql,
            new { ClientId = clientId, ModuleCode = moduleCode }, cancellationToken: cancellationToken));
        if (row is null || row.ModuleStatus != "active")
            return ModuleAccessDecision.Denied("MODULE_UNAVAILABLE", "Este módulo não está disponível no momento.");
        if (!row.RequiresContract) return ModuleAccessDecision.Granted();
        if (row.ContractStatus is not ("active" or "read_only" or "suspended" or "expired"))
            return ModuleAccessDecision.Denied("MODULE_NOT_CONTRACTED", "Este módulo não faz parte do plano contratado.");
        if (row.SubscriptionStatus is not ("active" or "trialing") || row.SubscriptionEndsAt is { } end && end <= DateTimeOffset.UtcNow)
            return writeOperation
                ? ModuleAccessDecision.Denied("SUBSCRIPTION_INACTIVE", "Sua assinatura permite consultar dados anteriores, mas novas ações estão temporariamente bloqueadas.")
                : ModuleAccessDecision.Granted(true);
        return row.ContractStatus switch
        {
            "active" => ModuleAccessDecision.Granted(),
            "read_only" or "suspended" or "expired" when !writeOperation => ModuleAccessDecision.Granted(true),
            "read_only" or "suspended" or "expired" => ModuleAccessDecision.Denied("MODULE_READ_ONLY", "Este módulo está disponível somente para consulta."),
            _ => ModuleAccessDecision.Denied("MODULE_NOT_CONTRACTED", "Este módulo não faz parte do plano contratado.")
        };
    }

    public async Task<bool> SetModuleStatusAsync(Guid clientId, string moduleCode, string status, Guid actorUserId,
        string reason, string correlationId, CancellationToken cancellationToken)
    {
        using var connection = connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        const string selectSql = """
            SELECT cs.id AS SubscriptionId,m.id AS ModuleId,m.access_module_code AS AccessModuleCode,csm.status AS PreviousStatus
              FROM valorapesquisa.client_subscriptions cs
              JOIN valorapesquisa.saas_modules m ON m.code=@ModuleCode AND m.status='active' AND m.deleted_at IS NULL
              LEFT JOIN valorapesquisa.client_subscription_modules csm ON csm.subscription_id=cs.id AND csm.module_id=m.id
             WHERE cs.client_id=@ClientId AND cs.deleted_at IS NULL ORDER BY cs.created_at DESC LIMIT 1 FOR UPDATE OF cs;
            """;
        var target = await connection.QuerySingleOrDefaultAsync<ModuleMutationRow>(new CommandDefinition(selectSql,
            new { ClientId = clientId, ModuleCode = moduleCode }, transaction, cancellationToken: cancellationToken));
        if (target is null) return false;

        const string upsertSql = """
            INSERT INTO valorapesquisa.client_subscription_modules(subscription_id,client_id,module_id,status,source,suspended_at,cancelled_at)
            VALUES(@SubscriptionId,@ClientId,@ModuleId,@Status,'manual',CASE WHEN @Status='suspended' THEN now() ELSE NULL END,
                   CASE WHEN @Status='cancelled' THEN now() ELSE NULL END)
            ON CONFLICT(subscription_id,module_id) DO UPDATE SET status=excluded.status,source='manual',
              suspended_at=excluded.suspended_at,cancelled_at=excluded.cancelled_at,updated_at=now();
            """;
        await connection.ExecuteAsync(new CommandDefinition(upsertSql, new
        {
            target.SubscriptionId,
            ClientId = clientId,
            target.ModuleId,
            Status = status
        }, transaction, cancellationToken: cancellationToken));

        const string bridgeSql = """
            INSERT INTO valorapesquisa.organization_modules(organization_id,module_id,module_code,enabled,source)
            SELECT @ClientId,id,code,@Enabled,'contract' FROM valorapesquisa.modules WHERE code=@AccessModuleCode
            ON CONFLICT(organization_id,module_code) DO UPDATE SET enabled=excluded.enabled,source='contract',updated_at=now();
            """;
        await connection.ExecuteAsync(new CommandDefinition(bridgeSql, new
        {
            ClientId = clientId,
            target.AccessModuleCode,
            Enabled = status is "active" or "read_only"
        }, transaction, cancellationToken: cancellationToken));

        const string auditSql = """
            INSERT INTO valorapesquisa.module_access_audit(client_id,module_id,actor_user_id,action,previous_status,new_status,reason,correlation_id)
            VALUES(@ClientId,@ModuleId,@ActorUserId,'module.contract.status_changed',@PreviousStatus,@Status,@Reason,@CorrelationId);
            INSERT INTO valorapesquisa.subscription_audit_events(client_id,subscription_id,actor_user_id,event_type,reason,correlation_id,before_jsonb,after_jsonb)
            VALUES(@ClientId,@SubscriptionId,@ActorUserId,'subscription.module.changed',@Reason,@CorrelationId,
                   jsonb_build_object('moduleCode',@ModuleCode,'status',@PreviousStatus),jsonb_build_object('moduleCode',@ModuleCode,'status',@Status));
            """;
        await connection.ExecuteAsync(new CommandDefinition(auditSql, new
        {
            ClientId = clientId,
            target.SubscriptionId,
            target.ModuleId,
            ActorUserId = actorUserId,
            ModuleCode = moduleCode,
            target.PreviousStatus,
            Status = status,
            Reason = reason,
            CorrelationId = correlationId
        }, transaction, cancellationToken: cancellationToken));
        transaction.Commit();
        return true;
    }

    public async Task RequestUpgradeAsync(Guid clientId, Guid actorUserId, string moduleCode, string reason,
        string correlationId, CancellationToken cancellationToken)
    {
        using var connection = connections.Create();
        const string sql = """
            INSERT INTO valorapesquisa.subscription_audit_events(client_id,subscription_id,actor_user_id,event_type,reason,correlation_id,after_jsonb)
            SELECT @ClientId,cs.id,@ActorUserId,'subscription.upgrade.requested',@Reason,@CorrelationId,
                   jsonb_build_object('moduleCode',@ModuleCode,'status','requested')
              FROM valorapesquisa.client_subscriptions cs
             WHERE cs.client_id=@ClientId AND cs.deleted_at IS NULL ORDER BY cs.created_at DESC LIMIT 1;
            """;
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql,
            new { ClientId = clientId, ActorUserId = actorUserId, ModuleCode = moduleCode, Reason = reason, CorrelationId = correlationId },
            cancellationToken: cancellationToken));
        if (affected != 1) throw new InvalidOperationException("Não foi possível localizar a assinatura ativa deste cliente.");
    }

    private sealed record ModuleAccessRow(bool RequiresContract, string ModuleStatus, string? SubscriptionStatus,
        DateTimeOffset? SubscriptionEndsAt, string? ContractStatus);

    private sealed record ModuleMutationRow(Guid SubscriptionId, Guid ModuleId, string AccessModuleCode, string? PreviousStatus);
}
