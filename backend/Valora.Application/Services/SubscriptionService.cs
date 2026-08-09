using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class SubscriptionService(ISubscriptionRepository repo) : ISubscriptionService
{
    private static readonly HashSet<string> Statuses = ["current", "awaiting_payment", "overdue", "delinquent", "suspended", "cancelled", "trial"];
    private static readonly HashSet<string> Cycles = ["monthly", "annual"];

    public Task<SubscriptionDto?> GetAsync(Guid organizationId) => repo.GetByOrganizationAsync(organizationId);
    public Task<IReadOnlyList<ManualPaymentDto>> ListPaymentsAsync(Guid organizationId) => repo.ListPaymentsAsync(organizationId);

    public async Task UpdateAsync(Guid organizationId, UpdateSubscriptionRequest request)
    {
        ValidateStatus(request.Status);
        if (!Cycles.Contains(request.BillingCycle)) throw new ArgumentException("Ciclo de cobrança inválido.");
        if (request.ContractedValue < 0 || request.DiscountValue < 0 || request.DiscountValue > request.ContractedValue)
            throw new ArgumentException("Valores da assinatura são inválidos.");
        await repo.UpsertAsync(new SubscriptionDto { Id = Guid.NewGuid(), OrganizationId = organizationId, PlanId = request.PlanId,
            Status = request.Status, BillingCycle = request.BillingCycle, ContractedValue = request.ContractedValue,
            DiscountValue = request.DiscountValue, StartsAt = request.StartsAt, RenewalAt = request.RenewalAt,
            DueAt = request.DueAt, FinancialContact = request.FinancialContact, FinancialEmail = request.FinancialEmail,
            FinancialPhone = request.FinancialPhone, PaymentMethod = request.PaymentMethod, Notes = request.Notes });
    }

    public Task SetStatusAsync(Guid organizationId, string status) { ValidateStatus(status); return repo.SetStatusAsync(organizationId, status); }

    public Task<ManualPaymentDto> RegisterPaymentAsync(Guid organizationId, Guid? userId, RegisterManualPaymentRequest request)
    {
        if (request.Amount <= 0) throw new ArgumentException("O pagamento deve ter valor maior que zero.");
        if (string.IsNullOrWhiteSpace(request.Method)) throw new ArgumentException("Informe a forma de pagamento.");
        return repo.RegisterPaymentAsync(organizationId, userId, request);
    }

    private static void ValidateStatus(string status)
    {
        if (!Statuses.Contains(status)) throw new ArgumentException("Status de assinatura inválido.");
    }
}
