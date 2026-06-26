using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;
public sealed class CommunicationRepository(IDbConnectionFactory f):ICommunicationRepository{ public async Task AddEmailJobAsync(Guid responseId,string toEmail,string status){using var c=f.Create(); await c.ExecuteAsync("INSERT INTO valorapesquisa.email_jobs(response_id,recipient_email,subject,template_key,status) VALUES (@responseId,@toEmail,'Valora Insight™ - Seu diagnóstico está pronto','result-ready',@status)",new{responseId,toEmail,status});} public async Task<IReadOnlyList<dynamic>> ListAsync(Guid? organizationId=null){using var c=f.Create(); var rows=await c.QueryAsync("SELECT * FROM valorapesquisa.communications WHERE (@organizationId IS NULL OR organization_id=@organizationId) ORDER BY created_at DESC LIMIT 100",new{organizationId}); return rows.ToList();}}
