using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

internal static class SafeData { public static string MaskEmail(string? email){ if(string.IsNullOrWhiteSpace(email)||!email.Contains('@')) return "***"; var p=email.Split('@'); return $"{p[0][0]}***@{p[1]}"; } public static string Hash(string? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value??""))).ToLowerInvariant(); }

public sealed class LgpdConsentService(ILgpdRepository repo,IAuditRepository audit):ILgpdConsentService{ public async Task<LgpdConsentDto> RegisterAsync(RegisterLgpdConsentRequest r,string? ip,string? ua){ var c=await repo.AddConsentAsync(r,SafeData.Hash(r.ParticipantEmail),ip,ua); await audit.AddAsync(new AuditEntry(r.OrganizationId,null,"lgpd.consent.registered","lgpd",c.Id.ToString(),"Consentimento registrado","{}")); return c;} public Task<IReadOnlyList<LgpdConsentDto>> ListAsync(Guid o)=>repo.ListConsentsAsync(o); }
