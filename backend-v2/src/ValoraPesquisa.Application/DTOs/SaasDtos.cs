namespace ValoraPesquisa.Application.DTOs;
public record PlanDto(Guid Id,string Code,string Name,string? Description,decimal MonthlyPrice,decimal AnnualPrice,string Status,int DisplayOrder,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt,bool IsDeleted);
public record PlanLimitDto(Guid Id,Guid PlanId,int ActiveSurveys,int ResponsesPerMonth,int Users,int Managers,int Forms,int PublicLinks,int EmailInvitesPerMonth,int StorageMb,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
public record PlanCapabilityDto(Guid Id,Guid PlanId,string CapabilityCode,bool Enabled);
public record ModuleDto(Guid Id,string Code,string Name,string? Description,string Category,string Status,int DisplayOrder,string? MinimumPlanCode,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt,bool IsDeleted);
public record OrganizationModuleDto(Guid Id,Guid OrganizationId,Guid ModuleId,string ModuleCode,string ModuleName,bool Enabled,string Source,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
public record SubscriptionDto(Guid Id,Guid OrganizationId,Guid PlanId,string PlanCode,string PlanName,string Status,string BillingStatus,DateTimeOffset StartsAt,DateTimeOffset? ExpiresAt,DateTimeOffset? TrialEndsAt,DateTimeOffset? CancelledAt,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
public record UsageMonthlyDto(Guid Id,Guid OrganizationId,DateTime Month,int ActiveSurveysCount,int ResponsesCount,int UsersCount,int ManagersCount,int FormsCount,int PublicLinksCount,int EmailInvitesCount,int StorageMbUsed,DateTimeOffset UpdatedAt);
public record EntitlementResultDto(bool Allowed,string Code,string Message,int? Limit=null,int? Used=null){ public static EntitlementResultDto Allow()=>new(true,"ALLOWED","Permitido."); public static EntitlementResultDto Deny(string code,string message,int? limit=null,int? used=null)=>new(false,code,message,limit,used); }
public record MenuItemDto(string Code,string Label,string Url,bool Enabled,string? Reason=null);
public record UsageIndicatorDto(string Code,string Label,int Used,int Limit,bool Unlimited);
public record DashboardGlobalDto(int TotalOrganizations,int ActiveOrganizations,IReadOnlyList<PlanCountDto> OrganizationsByPlan,int TotalUsers,int PublishedSurveys,int ResponsesThisMonth,int OrganizationsAtLimit,IReadOnlyList<AuditEventDto> RecentAuditEvents,int SuspendedOrExpiredSubscriptions);
public record DashboardOrganizationDto(Guid OrganizationId,string PlanCode,string PlanName,string SubscriptionStatus,IReadOnlyList<UsageIndicatorDto> Usage,IReadOnlyList<OrganizationModuleDto> EnabledModules,IReadOnlyList<ResponseDto> LatestResponses,IReadOnlyList<AuditEventDto> LatestAuditEvents,IReadOnlyList<string> NextSteps);
public record PlanCountDto(string PlanCode,int Count);
