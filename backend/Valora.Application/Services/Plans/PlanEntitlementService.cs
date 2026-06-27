using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;
public sealed class PlanEntitlementService(IPlanRepository plans, ISurveyRepository surveys, IOrganizationRepository orgs): IPlanEntitlementService{
 public async Task<PlanEntitlements> ResolveAsync(Guid organizationId){ var id=await plans.GetCurrentPlanIdAsync(organizationId)??"free"; var p=await plans.GetByIdAsync(id)??await plans.GetByIdAsync("free"); return new(id,p?.Limits??new(),p?.Capabilities??new()); }
 public async Task<bool> HasCapabilityAsync(Guid organizationId,string capability){ var e=await ResolveAsync(organizationId); return e.Capabilities.TryGetValue(capability,out var v)&&v!="none"; }
 public async Task<LimitCheckResult> CheckLimitAsync(Guid organizationId,string limitKey,int requestedAmount=1){ var usage=await GetUsageAsync(organizationId); var e=await ResolveAsync(organizationId); var limit=e.Limits.GetValueOrDefault(limitKey,-1); var current=limitKey switch {"activeSurveys"=>usage.ActiveSurveys,"responsesPerMonth"=>usage.ResponsesThisMonth,"managers"=>usage.Managers,_=>0}; return new(limit<0 || current+requestedAmount<=limit,limitKey,limit,current,requestedAmount); }
 public async Task<UsageDto> GetUsageAsync(Guid organizationId){ var e=await ResolveAsync(organizationId); return new(await surveys.CountActiveSurveysAsync(organizationId),await surveys.CountResponsesThisMonthAsync(organizationId),await orgs.CountManagersAsync(organizationId),e.Limits); }
}
