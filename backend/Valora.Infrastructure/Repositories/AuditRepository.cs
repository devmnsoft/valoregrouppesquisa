using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;
public sealed class AuditRepository(IDbConnectionFactory f):IAuditRepository{ public async Task AddAsync(AuditEntry e){using var c=f.Create(); await c.ExecuteAsync("INSERT INTO valorapesquisa.audit_logs(organization_id,user_id,action,entity_type,entity_id,message,metadata_json) VALUES (@OrganizationId,@UserId,@Action,@EntityType,@EntityId,@Message,CAST(@MetadataJson AS jsonb))",e);}}
