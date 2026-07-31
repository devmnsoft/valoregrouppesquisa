namespace Valora.Application.DTOs;

public sealed record OrganizationBrandingResponse(string PrimaryColor, string SecondaryColor, string? LogoUrl, string PublicSlug, bool WhiteLabelEnabled, long Version);
public sealed record UpdateOrganizationBrandingRequest(string PrimaryColor, string SecondaryColor, string? LogoUrl, string PublicSlug, bool WhiteLabelEnabled, long Version);
public sealed record OrganizationMetricResponse(string Key, string Label, string Period, long Consumed, long Reserved, long? Limit, long? Available, decimal? Percentage, bool Unlimited);
public sealed record OrganizationSubscriptionResponse(Guid SubscriptionId, string PlanCode, string PlanName, string Status, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, IReadOnlyList<string> Capabilities, IReadOnlyDictionary<string,long?> Limits, IReadOnlyList<OrganizationMetricResponse> Metrics);
public sealed record OnboardingStepResponse(string Code, string Label, string Description, string Status, DateTimeOffset? CompletedAt, string ActionUrl, bool Automatic, string RequiredPermission);
