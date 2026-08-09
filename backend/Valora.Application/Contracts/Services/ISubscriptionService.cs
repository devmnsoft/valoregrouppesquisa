using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetAsync(Guid organizationId);
    Task UpdateAsync(Guid organizationId, UpdateSubscriptionRequest request);
    Task SetStatusAsync(Guid organizationId,string status);
    Task<ManualPaymentDto> RegisterPaymentAsync(Guid organizationId, Guid? userId, RegisterManualPaymentRequest request);
    Task<IReadOnlyList<ManualPaymentDto>> ListPaymentsAsync(Guid organizationId);
}
