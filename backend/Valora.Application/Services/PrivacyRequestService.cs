using Valora.Application.Contracts;
using Valora.Application.Security;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class PrivacyRequestService(ILgpdRepository repo,IAuditRepository audit, ISensitiveDataSanitizer sanitizer):IPrivacyRequestService{ public async Task<PrivacyRequestDto> CreatePublicAsync(CreatePrivacyRequestRequest r){ var p=await repo.CreateRequestAsync(r,sanitizer.Hash(r.RequesterEmail),sanitizer.MaskEmail(r.RequesterEmail)); await audit.AddAsync(new AuditEntry(r.OrganizationId,null,"lgpd.privacy_request.created","privacy_request",p.Id.ToString(),"Solicitação LGPD criada","{}")); return p;} public Task<PrivacyRequestDto?> GetPublicAsync(string protocol)=>repo.GetRequestAsync(protocol); public Task<IReadOnlyList<PrivacyRequestDto>> ListAsync(Guid o)=>repo.ListRequestsAsync(o); public Task UpdateStatusAsync(Guid o,Guid id,string s,Guid? h)=>repo.UpdateStatusAsync(o,id,s,h); public async Task CompleteAsync(Guid o,Guid id,string r,Guid? h){ await repo.CompleteAsync(o,id,r,h); await audit.AddAsync(new AuditEntry(o,h,"lgpd.privacy_request.completed","privacy_request",id.ToString(),"Solicitação LGPD concluída","{}")); }}
