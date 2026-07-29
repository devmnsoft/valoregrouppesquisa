using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

internal static class SafeData { public static string MaskEmail(string? email){ if(string.IsNullOrWhiteSpace(email)||!email.Contains('@')) return "***"; var p=email.Split('@'); return $"{p[0][0]}***@{p[1]}"; } public static string Hash(string? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value??""))).ToLowerInvariant(); }

public sealed class EntitlementService(IModuleRepository modules, ISubscriptionRepository subscriptions, IAuditRepository audit) : IEntitlementService { public async Task<EntitlementDto> ResolveAsync(Guid organizationId){ var mods=await modules.ListForOrganizationAsync(organizationId); return new EntitlementDto(organizationId,"official",mods.Where(m=>m.Status=="active").Select(m=>m.Code).ToArray(),new Dictionary<string,int>()); } public async Task<bool> CanUseAsync(Guid organizationId,string moduleCode){ var sub=await subscriptions.GetByOrganizationAsync(organizationId); if(sub is not null && sub.Status is not "active" and not "trialing"){ await audit.AddAsync(new AuditEntry(organizationId,null,"entitlement.blocked.subscription","subscription",organizationId.ToString(),"SUBSCRIPTION_INACTIVE","{}")); return false; } var ok=(await modules.ListForOrganizationAsync(organizationId)).Any(m=>m.Code==moduleCode && m.Status=="active"); if(!ok) await audit.AddAsync(new AuditEntry(organizationId,null,"entitlement.blocked.module","module",moduleCode,"MODULE_NOT_ENABLED","{}")); return ok; }}
