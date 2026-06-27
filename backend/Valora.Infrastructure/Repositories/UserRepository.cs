using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.Security;
namespace Valora.Infrastructure.Repositories;
public sealed class UserRepository(IDbConnectionFactory f, ILogger<UserRepository> logger):IUserRepository
{
 public async Task<dynamic?> GetByEmailAsync(string email){try{using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT * FROM valorapesquisa.users WHERE lower(email)=lower(@email) AND is_deleted=false",new{email});}catch(Exception ex){logger.LogError(ex,"Erro ao buscar usuário por e-mail. Email={Email}",LogSanitizer.MaskEmail(email));throw;}}
 public async Task<dynamic?> GetAsync(Guid id){try{using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT id,organization_id,name,email,role,status FROM valorapesquisa.users WHERE id=@id",new{id});}catch(Exception ex){logger.LogError(ex,"Erro ao buscar usuário. UserId={UserId}",id);throw;}}
 public async Task<Guid> CreateAsync(Guid organizationId,string name,string email,string passwordHash,string role){try{using var c=f.Create(); return await c.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.users(organization_id,name,email,password_hash,role) VALUES (@organizationId,@name,@email,@passwordHash,@role) RETURNING id",new{organizationId,name,email,passwordHash,role});}catch(Exception ex){logger.LogError(ex,"Erro ao criar usuário. OrganizationId={OrganizationId} Email={Email} Role={Role}",organizationId,LogSanitizer.MaskEmail(email),role);throw;}}
 public async Task TouchLoginAsync(Guid id){try{using var c=f.Create(); await c.ExecuteAsync("UPDATE valorapesquisa.users SET last_login_at=now() WHERE id=@id",new{id});}catch(Exception ex){logger.LogError(ex,"Erro ao atualizar último login. UserId={UserId}",id);throw;}}
}
