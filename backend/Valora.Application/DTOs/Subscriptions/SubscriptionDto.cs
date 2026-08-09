namespace Valora.Application.DTOs;

public sealed class SubscriptionDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid PlanId { get; init; }
    public string Status { get; init; } = "awaiting_payment";
    public string BillingCycle { get; init; } = "monthly";
    public decimal ContractedValue { get; init; }
    public decimal DiscountValue { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset? RenewalAt { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public string? FinancialContact { get; init; }
    public string? FinancialEmail { get; init; }
    public string? FinancialPhone { get; init; }
    public string? PaymentMethod { get; init; }
    public string? Notes { get; init; }
    public int? TrialDaysRemaining { get; init; }
    public bool AccessBlocked => Status is "delinquent" or "suspended" or "cancelled";
}

public sealed record UpdateSubscriptionRequest(Guid PlanId, string Status, string BillingCycle,
    decimal ContractedValue, decimal DiscountValue, DateTimeOffset StartsAt, DateTimeOffset? RenewalAt,
    DateTimeOffset? DueAt, string? FinancialContact, string? FinancialEmail, string? FinancialPhone,
    string? PaymentMethod, string? Notes);

public sealed record RegisterManualPaymentRequest(decimal Amount, DateTimeOffset PaidAt, string Method,
    string? Reference, string? Notes);

public sealed record ManualPaymentDto(Guid Id, Guid SubscriptionId, decimal Amount, DateTimeOffset PaidAt,
    string Method, string? Reference, string? Notes, Guid? RegisteredBy, DateTimeOffset CreatedAt);
