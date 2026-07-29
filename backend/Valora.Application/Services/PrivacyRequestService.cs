using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

internal static class SafeData { public static string MaskEmail(string? email){ if(string.IsNullOrWhiteSpace(email)||!email.Contains('@')) return "***"; var p=email.Split('@'); return $"{p[0][0]}***@{p[1]}"; } public static string Hash(string? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value??""))).ToLowerInvariant(); }

public sealed class PrivacyRequestService(ILgpdRepository repo,IAuditRepository audit):IPrivacyRequestService{ public async Task<PrivacyRequestDto> CreatePublicAsync(CreatePrivacyRequestRequest r){ var p=await repo.CreateRequestAsync(r,SafeData.Hash(r.RequesterEmail),SafeData.MaskEmail(r.RequesterEmail)); await audit.AddAsync(new AuditEntry(r.OrganizationId,null,"lgpd.privacy_request.created","privacy_request",p.Id.ToString(),"Solicitação LGPD criada","{}")); return p;} public Task<PrivacyRequestDto?> GetPublicAsync(string protocol)=>repo.GetRequestAsync(protocol); public Task<IReadOnlyList<PrivacyRequestDto>> ListAsync(Guid o)=>repo.ListRequestsAsync(o); public Task UpdateStatusAsync(Guid o,Guid id,string s,Guid? h)=>repo.UpdateStatusAsync(o,id,s,h); public async Task CompleteAsync(Guid o,Guid id,string r,Guid? h){ await repo.CompleteAsync(o,id,r,h); await audit.AddAsync(new AuditEntry(o,h,"lgpd.privacy_request.completed","privacy_request",id.ToString(),"Solicitação LGPD concluída","{}")); }}
