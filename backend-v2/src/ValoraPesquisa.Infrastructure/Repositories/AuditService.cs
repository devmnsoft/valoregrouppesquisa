using System.Text.Json;
using Dapper;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Infrastructure.Database;

namespace ValoraPesquisa.Infrastructure.Repositories;
public sealed class AuditService(PostgresConnectionFactory db):IAuditService{
 static string Clean(object? metadata){ var json=JsonSerializer.Serialize(metadata??new{}); foreach(var word in new[]{"password","senha","token","hash","secret","segredo"}) json=json.Replace(word,"redacted",StringComparison.OrdinalIgnoreCase); return json; }
 public async Task RecordAsync(Guid? orgId,Guid? userId,string action,string entity,Guid? entityId,string correlationId,object? metadata=null){ using var c=db.Create(); await c.ExecuteAsync("insert into valorapesquisa.audit_logs(id,organization_id,user_id,action,entity,entity_id,correlation_id,metadata,created_at) values(gen_random_uuid(),@orgId,@userId,@action,@entity,@entityId,@correlationId,cast(@metadata as jsonb),now())",new{orgId,userId,action,entity,entityId,correlationId,metadata=Clean(metadata)}); }
 public async Task<IReadOnlyList<AuditEventDto>> ListAsync(CurrentUser u){ using var c=db.Create(); return (await c.QueryAsync<AuditEventDto>(u.IsAdminValora?"select id,organization_id OrganizationId,user_id UserId,action,entity,entity_id EntityId,correlation_id CorrelationId,metadata::text Metadata,created_at CreatedAt from valorapesquisa.audit_logs order by created_at desc limit 200":"select id,organization_id OrganizationId,user_id UserId,action,entity,entity_id EntityId,correlation_id CorrelationId,metadata::text Metadata,created_at CreatedAt from valorapesquisa.audit_logs where organization_id=@OrganizationId order by created_at desc limit 200",u)).ToList(); }}
