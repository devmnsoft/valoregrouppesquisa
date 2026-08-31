using System.Net;
using Valora.Application.Contracts;
using Valora.Application.Security;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class CertificateOperationalService(ICertificateOperationalRepository repo,IEntitlementService ent,IAuditRepository audit, ISensitiveDataSanitizer sanitizer):ICertificateOperationalService{ public async Task<CertificateDto> GenerateAsync(Guid o,Guid r){ if(!await ent.CanUseAsync(o,"certificados")) throw new InvalidOperationException("MODULE_NOT_ENABLED"); var code=$"VALORA-{Guid.NewGuid():N}"[..20].ToUpperInvariant(); var c=await repo.CreateAsync(o,null,r,"Participante",sanitizer.MaskEmail(null),"Empresa",null,"Emitido",code,$"/public/certificates/validate?code={code}","{}"); await audit.AddAsync(new AuditEntry(o,null,"certificate.generated","response",r.ToString(),"Certificado gerado","{}")); return c;} public Task<IReadOnlyList<CertificateDto>> ListAsync(Guid o)=>repo.ListAsync(o); public Task<CertificateDto?> GetAsync(Guid o,Guid id)=>repo.GetAsync(o,id); public async Task<string?> DownloadHtmlAsync(Guid o,Guid id){ var c=await repo.GetAsync(o,id); if(c is null||!string.Equals(c.Status,"issued",StringComparison.OrdinalIgnoreCase))return null; await audit.AddAsync(new AuditEntry(o,null,"certificate.downloaded","certificate",id.ToString(),"Certificado baixado","{}")); return $"<html><body><h1>Certificado Valora</h1><p>{WebUtility.HtmlEncode(c.ParticipantName)}</p><p>{WebUtility.HtmlEncode(c.ValidationCode)}</p></body></html>";} public async Task RevokeAsync(Guid o,Guid id){ await repo.RevokeAsync(o,id); await audit.AddAsync(new AuditEntry(o,null,"certificate.revoked","certificate",id.ToString(),"Certificado revogado","{}")); }}
