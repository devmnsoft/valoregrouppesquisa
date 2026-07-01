namespace Valora.Application.DTOs;

public sealed record OrganizationDto(Guid Id,string Name,string? PublicName,string? Slug,string? Email,string Status,string? PlanCode);
public sealed record UserDto(Guid Id,Guid? OrganizationId,string Name,string Email,string Role,string Status,string? Phone);
public sealed record FormDto(Guid Id,Guid OrganizationId,string Title,string? Description,string Status,IReadOnlyList<QuestionDto> Questions);
public sealed record QuestionDto(Guid Id,Guid FormId,string Text,string Type,int Position,bool Required,decimal Weight,IReadOnlyList<QuestionOptionDto> Options);
public sealed record QuestionOptionDto(Guid Id,Guid QuestionId,string Text,decimal Score,int Position);
public sealed record SurveyDto(Guid Id,Guid OrganizationId,Guid FormId,string Title,string? Description,string Status,DateTimeOffset? StartsAt,DateTimeOffset? ExpiresAt);
public sealed record SurveyLinkDto(Guid Id,Guid SurveyId,string PublicUrl,string Status,DateTimeOffset? ExpiresAt,DateTimeOffset? RevokedAt);
public sealed record ResponseDto(Guid Id,Guid OrganizationId,Guid SurveyId,Guid FormId,string? ParticipantName,string? ParticipantEmail,string Status,DateTimeOffset? CompletedAt);
public sealed record AuditEventDto(Guid Id,Guid? OrganizationId,Guid? UserId,string Action,string Entity,Guid? EntityId,string CorrelationId,DateTimeOffset CreatedAt);
public sealed record ModuleDto(Guid Id,string Code,string Name,string? Description,string Category,string Status,int DisplayOrder,string? MinimumPlanCode);
public sealed record SubscriptionDto(Guid Id,Guid OrganizationId,Guid PlanId,string Status,string BillingStatus,DateTimeOffset StartsAt,DateTimeOffset? ExpiresAt);
public sealed record DashboardMetricsDto(int Organizations,int Users,int Surveys,int Responses,int ActiveSubscriptions);
public sealed record MenuItemDto(string Code,string Label,string Url,string Icon,int Order,IReadOnlyList<MenuItemDto> Children);
public sealed record EntitlementDto(Guid OrganizationId,string PlanCode,IReadOnlyList<string> EnabledModules,IReadOnlyDictionary<string,int> Limits);
