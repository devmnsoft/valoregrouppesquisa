using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class EmailQueueService(IEmailOperationalRepository repo,IEntitlementService ent,IAuditRepository audit):IEmailQueueService{ async Task<EmailJobDto> Q(Guid o,Guid? r,Guid? c,string t,string to,string s){ if(!await ent.CanUseAsync(o,"convites_email")) throw new InvalidOperationException("MODULE_NOT_ENABLED"); var j=await repo.QueueAsync(o,r,c,t,to,s,$"<p>{s}</p>",s); await audit.AddAsync(new AuditEntry(o,null,"email.queued","email_job",j.Id.ToString(),"E-mail enfileirado","{}")); return j;} public Task<EmailJobDto> QueueResultAsync(Guid o,Guid r,string to)=>Q(o,r,null,"result_available",to,"Resultado disponível"); public Task<EmailJobDto> QueueCertificateAsync(Guid o,Guid c,string to)=>Q(o,null,c,"certificate_issued",to,"Certificado emitido"); public Task<EmailJobDto> QueueInviteAsync(Guid o,Guid s,string to)=>Q(o,null,null,"survey_invite",to,"Convite para pesquisa"); public async Task<EmailJobDto?> RetryAsync(Guid id)=>await repo.GetJobAsync(id); }
