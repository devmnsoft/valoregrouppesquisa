namespace Valora.Application.Contracts.Billing;

/// <summary>
/// Boundary for a future payment provider. The public portal must not infer payment approval while the
/// installation is operating in manual mode.
/// </summary>
public interface IPaymentGateway
{
    bool IsConfigured { get; }
    Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default);
}

public sealed record PaymentCheckoutRequest(Guid OrganizationId, string PlanCode, string BillingCycle, Uri ReturnUrl);
public sealed record PaymentCheckoutResult(bool Created, Uri? CheckoutUrl, string Mode, string Message);
