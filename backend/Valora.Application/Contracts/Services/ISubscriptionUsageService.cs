using Valora.Application.DTOs;

namespace Valora.Application.Contracts;

public interface ISubscriptionUsageService
{
    Task<SubscriptionUsageDto> GetCurrentAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<SubscriptionLimitDecision> CheckAsync(Guid organizationId, string metric, int amount = 1,
        bool isValoraAdmin = false, CancellationToken cancellationToken = default);
}
