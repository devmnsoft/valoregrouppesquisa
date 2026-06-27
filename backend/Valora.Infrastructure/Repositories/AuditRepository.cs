using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
namespace Valora.Infrastructure.Repositories;
public sealed class AuditRepository(IDbConnectionFactory f, ILogger<AuditRepository> logger):IAuditRepository{ public async Task AddAsync(AuditEntry e)=>await LogAsync(e); public async Task LogAsync(AuditEntry e,IDbTransaction? tx=null){try{const string sql="INSERT INTO valorapesquisa.audit_logs(organization_id,user_id,action,entity_type,entity_id,message,metadata_json) VALUES (@OrganizationId,@UserId,@Action,@EntityType,@EntityId,@Message,CAST(@MetadataJson AS jsonb))"; if(tx?.Connection is not null) await tx.Connection.ExecuteAsync(sql,e,tx); else {using var c=f.Create(); await c.ExecuteAsync(sql,e);} } catch(Exception ex){ logger.LogError(ex,"Erro ao registrar auditoria de negócio. Action={Action} EntityType={EntityType} EntityId={EntityId}",e.Action,e.EntityType,e.EntityId); throw;}}}
