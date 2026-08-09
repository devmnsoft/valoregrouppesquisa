namespace Valora.Application.Contracts;
public interface ISubscriptionRepository
{
    Task<Valora.Application.DTOs.SubscriptionDto?> GetByOrganizationAsync(Guid organizationId);
    Task UpsertAsync(Valora.Application.DTOs.SubscriptionDto subscription);
    Task SetStatusAsync(Guid organizationId,string status);
    Task<Valora.Application.DTOs.ManualPaymentDto> RegisterPaymentAsync(Guid organizationId, Guid? userId, Valora.Application.DTOs.RegisterManualPaymentRequest request);
    Task<IReadOnlyList<Valora.Application.DTOs.ManualPaymentDto>> ListPaymentsAsync(Guid organizationId);
}
