using Valora.Application.DTOs;

namespace Valora.Application.Contracts;

public interface IOrganizationBrandingRepository
{
    Task<OrganizationBrandingResponse> GetAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> HasCapabilityAsync(Guid organizationId, string capability, CancellationToken cancellationToken = default);
    Task<OrganizationBrandingResponse?> UpdateAsync(Guid organizationId, UpdateOrganizationBrandingRequest request, CancellationToken cancellationToken = default);
    Task<OrganizationSubscriptionResponse?> GetSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OnboardingStepResponse>> GetOnboardingAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> CompleteStepAsync(Guid organizationId, string stepCode, CancellationToken cancellationToken = default);
}
