using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class SubscriptionRepository(IDbConnectionFactory connections) : ISubscriptionRepository
{
    private const string Projection = """id AS Id, organization_id AS OrganizationId, plan_id AS PlanId, status AS Status,
        billing_cycle AS BillingCycle, contracted_value AS ContractedValue, discount_value AS DiscountValue,
        starts_at AS StartsAt, renewal_at AS RenewalAt, due_at AS DueAt, ends_at AS EndsAt,
        financial_contact AS FinancialContact, financial_email AS FinancialEmail, financial_phone AS FinancialPhone,
        payment_method AS PaymentMethod, notes AS Notes""";

    public async Task<SubscriptionDto?> GetByOrganizationAsync(Guid organizationId)
    {
        using var connection = connections.Create();
        return await connection.QueryFirstOrDefaultAsync<SubscriptionDto>($"SELECT {Projection} FROM valorapesquisa.subscriptions WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1", new { organizationId });
    }

    public async Task UpsertAsync(SubscriptionDto value)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync("""INSERT INTO valorapesquisa.subscriptions
            (id,organization_id,plan_id,status,billing_cycle,contracted_value,discount_value,starts_at,renewal_at,due_at,financial_contact,financial_email,financial_phone,payment_method,notes)
            VALUES(@Id,@OrganizationId,@PlanId,@Status,@BillingCycle,@ContractedValue,@DiscountValue,@StartsAt,@RenewalAt,@DueAt,@FinancialContact,@FinancialEmail,@FinancialPhone,@PaymentMethod,@Notes)
            ON CONFLICT (organization_id) WHERE deleted_at IS NULL DO UPDATE SET plan_id=EXCLUDED.plan_id,status=EXCLUDED.status,
            billing_cycle=EXCLUDED.billing_cycle,contracted_value=EXCLUDED.contracted_value,discount_value=EXCLUDED.discount_value,
            starts_at=EXCLUDED.starts_at,renewal_at=EXCLUDED.renewal_at,due_at=EXCLUDED.due_at,financial_contact=EXCLUDED.financial_contact,
            financial_email=EXCLUDED.financial_email,financial_phone=EXCLUDED.financial_phone,payment_method=EXCLUDED.payment_method,
            notes=EXCLUDED.notes,updated_at=now()""", value);
    }

    public async Task SetStatusAsync(Guid organizationId, string status)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync("UPDATE valorapesquisa.subscriptions SET status=@status,updated_at=now() WHERE organization_id=@organizationId AND deleted_at IS NULL", new { organizationId, status });
    }

    public async Task<ManualPaymentDto> RegisterPaymentAsync(Guid organizationId, Guid? userId, RegisterManualPaymentRequest request)
    {
        using var connection = connections.Create();
        const string sql = """INSERT INTO valorapesquisa.manual_payments(id,subscription_id,organization_id,amount,paid_at,method,reference,notes,registered_by)
            SELECT gen_random_uuid(),id,organization_id,@Amount,@PaidAt,@Method,@Reference,@Notes,@userId FROM valorapesquisa.subscriptions
            WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1
            RETURNING id AS Id,subscription_id AS SubscriptionId,amount AS Amount,paid_at AS PaidAt,method AS Method,reference AS Reference,notes AS Notes,registered_by AS RegisteredBy,created_at AS CreatedAt""";
        return await connection.QuerySingleAsync<ManualPaymentDto>(sql, new { organizationId, userId, request.Amount, request.PaidAt, request.Method, request.Reference, request.Notes });
    }

    public async Task<IReadOnlyList<ManualPaymentDto>> ListPaymentsAsync(Guid organizationId)
    {
        using var connection = connections.Create();
        var items = await connection.QueryAsync<ManualPaymentDto>("""SELECT id AS Id,subscription_id AS SubscriptionId,amount AS Amount,paid_at AS PaidAt,method AS Method,reference AS Reference,notes AS Notes,registered_by AS RegisteredBy,created_at AS CreatedAt FROM valorapesquisa.manual_payments WHERE organization_id=@organizationId ORDER BY paid_at DESC""", new { organizationId });
        return items.AsList();
    }
}
