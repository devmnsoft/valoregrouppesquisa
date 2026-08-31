using Dapper;
using Valora.Application.Contracts;
using Valora.Application.SaasAdministration;

namespace Valora.Infrastructure.SaasAdministration;

public sealed class SaasCustomerRepository(IDbConnectionFactory factory) : ISaasCustomerRepository
{
    private const string Projection = "id Id, organization_id OrganizationId, legal_name LegalName, trade_name TradeName, tax_id_normalized TaxIdNormalized, plan_code PlanCode, status Status, created_at CreatedAt";

    public async Task<IReadOnlyList<SaasCustomerDto>> ListAsync(CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        var command = new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.saas_customers ORDER BY trade_name", cancellationToken: cancellationToken);
        return (await connection.QueryAsync<SaasCustomerDto>(command)).AsList();
    }

    public async Task<SaasCustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        var command = new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.saas_customers WHERE id=@id", new { id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<SaasCustomerDto>(command);
    }

    public async Task<SaasCustomerDto> CreateAsync(Guid id, CreateSaasCustomerRequest request, string normalizedTaxId, CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        const string sql = """INSERT INTO valorapesquisa.saas_customers(id,organization_id,legal_name,trade_name,tax_id_normalized,plan_code) VALUES(@id,@OrganizationId,@LegalName,@TradeName,@normalizedTaxId,@PlanCode) RETURNING id Id,organization_id OrganizationId,legal_name LegalName,trade_name TradeName,tax_id_normalized TaxIdNormalized,plan_code PlanCode,status Status,created_at CreatedAt""";
        return await connection.QuerySingleAsync<SaasCustomerDto>(new CommandDefinition(sql, new { id, request.OrganizationId, request.LegalName, request.TradeName, normalizedTaxId, request.PlanCode }, cancellationToken: cancellationToken));
    }

    public async Task<bool> SetBlockedAsync(Guid id, bool blocked, Guid actorUserId, string reason, string correlationId, CancellationToken cancellationToken)
    {
        using var connection = factory.Create(); connection.Open(); using var transaction = connection.BeginTransaction();
        const string update = "UPDATE valorapesquisa.saas_customers SET status=CASE WHEN @blocked THEN 'blocked' ELSE 'active' END,blocked_at=CASE WHEN @blocked THEN now() ELSE NULL END,block_reason=CASE WHEN @blocked THEN @reason ELSE NULL END,updated_at=now() WHERE id=@id";
        var changed = await connection.ExecuteAsync(new CommandDefinition(update, new { id, blocked, reason }, transaction, cancellationToken: cancellationToken)) == 1;
        if (changed)
        {
            const string audit = """INSERT INTO valorapesquisa.saas_admin_actions(customer_id,organization_id,actor_user_id,action,target_type,target_id,reason,correlation_id,after_json) SELECT id,organization_id,@actorUserId,@action,'customer',id,@reason,@correlationId,jsonb_build_object('status',@status) FROM valorapesquisa.saas_customers WHERE id=@id""";
            await connection.ExecuteAsync(new CommandDefinition(audit, new { id, actorUserId, action = blocked ? "saas.customer.blocked" : "saas.customer.unblocked", reason, correlationId, status = blocked ? "blocked" : "active" }, transaction, cancellationToken: cancellationToken));
        }
        transaction.Commit(); return changed;
    }
}
