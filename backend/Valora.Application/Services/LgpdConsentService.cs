using Valora.Application.Contracts;
using Valora.Application.Security;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class LgpdConsentService(ILgpdRepository repo,IAuditRepository audit, ISensitiveDataSanitizer sanitizer):ILgpdConsentService{ public async Task<LgpdConsentDto> RegisterAsync(RegisterLgpdConsentRequest r,string? ip,string? ua){ var c=await repo.AddConsentAsync(r,sanitizer.Hash(r.ParticipantEmail),ip,ua); await audit.AddAsync(new AuditEntry(r.OrganizationId,null,"lgpd.consent.registered","lgpd",c.Id.ToString(),"Consentimento registrado","{}")); return c;} public Task<IReadOnlyList<LgpdConsentDto>> ListAsync(Guid o)=>repo.ListConsentsAsync(o); }
