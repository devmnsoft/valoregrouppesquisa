namespace Valora.Application.DTOs;

public sealed record SubscriptionDto(Guid Id,Guid OrganizationId,Guid PlanId,string Status,DateTimeOffset StartsAt,DateTimeOffset? EndsAt);
