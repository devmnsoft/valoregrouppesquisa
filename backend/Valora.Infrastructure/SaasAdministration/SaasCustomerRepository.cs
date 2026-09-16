using Dapper;
using Valora.Application.Contracts;
using Valora.Application.SaasAdministration;

namespace Valora.Infrastructure.SaasAdministration;

public sealed class SaasCustomerRepository(IDbConnectionFactory factory) : ISaasCustomerRepository {
    private const string Projection = "id Id, organization_id OrganizationId, legal_name LegalName, trade_name TradeName, tax_id_normalized TaxIdNormalized, plan_code PlanCode, status Status, created_at CreatedAt";

    public async Task<IReadOnlyList<SaasCustomerDto>> ListAsync(CancellationToken cancellationToken) {
        using var connection = factory.Create();
        var command = new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.saas_customers ORDER BY trade_name", cancellationToken: cancellationToken);
        return (await connection.QueryAsync<SaasCustomerDto>(command)).AsList();
    }

    public async Task<SaasCustomerPage> ListPageAsync(SaasCustomerListQuery query, CancellationToken cancellationToken) {
        using var connection = factory.Create();
        var direction = query.Descending ? "DESC" : "ASC";
        var order = query.Sort switch { "created" => $"c.created_at {direction}, c.id ASC", "activity" => $"activity.last_activity_at {direction} NULLS LAST, c.trade_name ASC, c.id ASC", _ => $"c.trade_name {direction}, c.id ASC" };
        var where = """WHERE (@search IS NULL OR c.trade_name ILIKE '%'||@search||'%' OR c.legal_name ILIKE '%'||@search||'%' OR c.tax_id_normalized LIKE '%'||NULLIF(regexp_replace(@search,'[^0-9]','','g'),'')||'%') AND (@status IS NULL OR c.status=@status) AND (@module IS NULL OR EXISTS (SELECT 1 FROM valorapesquisa.saas_customer_modules fm WHERE fm.customer_id=c.id AND fm.module_code=@module AND fm.enabled))""";
        var sql = $"""
            SELECT count(*) FROM valorapesquisa.saas_customers c {where};
            SELECT c.id Id,c.organization_id OrganizationId,c.legal_name LegalName,c.trade_name TradeName,
                   CASE WHEN length(c.tax_id_normalized)=14 THEN '••.•••.•••/••'||right(c.tax_id_normalized,4) ELSE '•••.•••.•••-'||right(c.tax_id_normalized,2) END TaxIdMasked,
                   c.plan_code PlanCode,c.status Status,c.created_at CreatedAt,COALESCE(users.active_user_count,0)::int ActiveUserCount,
                   COALESCE(modules.active_modules,ARRAY[]::text[]) ActiveModules,activity.last_activity_at LastActivityAt
            FROM valorapesquisa.saas_customers c
            LEFT JOIN LATERAL (SELECT count(*)::int active_user_count FROM valorapesquisa.saas_customer_users u WHERE u.customer_id=c.id AND u.status='active') users ON true
            LEFT JOIN LATERAL (SELECT array_agg(m.module_code ORDER BY m.module_code) active_modules FROM valorapesquisa.saas_customer_modules m WHERE m.customer_id=c.id AND m.enabled) modules ON true
            LEFT JOIN LATERAL (SELECT max(a.created_at) last_activity_at FROM valorapesquisa.saas_customer_audit_events a WHERE a.customer_id=c.id) activity ON true
            {where} ORDER BY {order} OFFSET @offset LIMIT @pageSize;
            """;
        var parameters = new { search = query.Search, status = query.Status, module = query.Module, offset = (query.Page - 1) * query.PageSize, pageSize = query.PageSize };
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        var total = await result.ReadSingleAsync<int>();
        return new SaasCustomerPage((await result.ReadAsync<SaasCustomerListItem>()).AsList(), total, query.Page, query.PageSize);
    }

    public async Task<SaasCustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken) {
        using var connection = factory.Create();
        var command = new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.saas_customers WHERE id=@id", new { id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<SaasCustomerDto>(command);
    }

    public async Task<SaasCustomerDto> CreateAsync(Guid id, CreateSaasCustomerRequest request, string normalizedTaxId, CancellationToken cancellationToken) {
        using var connection = factory.Create();
        const string sql = """INSERT INTO valorapesquisa.saas_customers(id,organization_id,legal_name,trade_name,tax_id_normalized,plan_code) VALUES(@id,@OrganizationId,@LegalName,@TradeName,@normalizedTaxId,@PlanCode) RETURNING id Id,organization_id OrganizationId,legal_name LegalName,trade_name TradeName,tax_id_normalized TaxIdNormalized,plan_code PlanCode,status Status,created_at CreatedAt""";
        return await connection.QuerySingleAsync<SaasCustomerDto>(new CommandDefinition(sql, new { id, request.OrganizationId, request.LegalName, request.TradeName, normalizedTaxId, request.PlanCode }, cancellationToken: cancellationToken));
    }

    public async Task<bool> SetBlockedAsync(Guid id, bool blocked, Guid actorUserId, string reason, string correlationId, CancellationToken cancellationToken) {
        using var connection = factory.Create(); connection.Open(); using var transaction = connection.BeginTransaction();
        const string update = "UPDATE valorapesquisa.saas_customers SET status=CASE WHEN @blocked THEN 'blocked' ELSE 'active' END,blocked_at=CASE WHEN @blocked THEN now() ELSE NULL END,block_reason=CASE WHEN @blocked THEN @reason ELSE NULL END,updated_at=now() WHERE id=@id";
        var changed = await connection.ExecuteAsync(new CommandDefinition(update, new { id, blocked, reason }, transaction, cancellationToken: cancellationToken)) == 1;
        if (changed) {
            const string audit = """INSERT INTO valorapesquisa.saas_admin_actions(customer_id,organization_id,actor_user_id,action,target_type,target_id,reason,correlation_id,after_json) SELECT id,organization_id,@actorUserId,@action,'customer',id,@reason,@correlationId,jsonb_build_object('status',@status) FROM valorapesquisa.saas_customers WHERE id=@id""";
            await connection.ExecuteAsync(new CommandDefinition(audit, new { id, actorUserId, action = blocked ? "saas.customer.blocked" : "saas.customer.unblocked", reason, correlationId, status = blocked ? "blocked" : "active" }, transaction, cancellationToken: cancellationToken));
        }
        transaction.Commit(); return changed;
    }
}
