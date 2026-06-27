using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Security;
namespace Valora.Infrastructure.Repositories;
public sealed class OrganizationRepository(IDbConnectionFactory f, ILogger<OrganizationRepository> logger):IOrganizationRepository
{
 public async Task<dynamic?> GetAsync(Guid id){try{using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT id,name,public_name,email,phone,document,status,plan_id FROM valorapesquisa.organizations WHERE id=@id AND is_deleted=false",new{id});}catch(Exception ex){logger.LogError(ex,"Erro ao buscar organização. OrganizationId={OrganizationId}",id);throw;}}
 public async Task<Guid> CreateAsync(string name,string email,string slug,string planId){try{using var c=f.Create(); return await c.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.organizations(name,public_name,email,slug,plan_id) VALUES (@name,@name,@email,@slug,@planId) RETURNING id",new{name,email,slug,planId});}catch(Exception ex){logger.LogError(ex,"Erro ao criar organização. Email={Email} PlanId={PlanId}",LogSanitizer.MaskEmail(email),planId);throw;}}
 public async Task UpdateCurrentAsync(Guid id,UpdateOrganizationRequest r){try{using var c=f.Create(); await c.ExecuteAsync("UPDATE valorapesquisa.organizations SET public_name=COALESCE(@PublicName,public_name), phone=COALESCE(@Phone,phone), document=COALESCE(@Document,document), updated_at=now() WHERE id=@id",new{r.PublicName,r.Phone,r.Document,id});}catch(Exception ex){logger.LogError(ex,"Erro ao atualizar organização. OrganizationId={OrganizationId} Phone={Phone} Document={Document}",id,LogSanitizer.MaskPhone(r.Phone),LogSanitizer.MaskDocument(r.Document));throw;}}
 public async Task<int> CountManagersAsync(Guid organizationId){try{using var c=f.Create(); return await c.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.users WHERE organization_id=@organizationId AND is_deleted=false AND status='active'",new{organizationId});}catch(Exception ex){logger.LogError(ex,"Erro ao contar gestores. OrganizationId={OrganizationId}",organizationId);throw;}}
}
