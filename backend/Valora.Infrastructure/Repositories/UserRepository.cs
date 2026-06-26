using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;
public sealed class UserRepository(IDbConnectionFactory f):IUserRepository{ public async Task<dynamic?> GetByEmailAsync(string email){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT * FROM valorapesquisa.users WHERE lower(email)=lower(@email) AND is_deleted=false",new{email});} public async Task<dynamic?> GetAsync(Guid id){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT id,organization_id,name,email,role,status FROM valorapesquisa.users WHERE id=@id",new{id});} public async Task<Guid> CreateAsync(Guid organizationId,string name,string email,string passwordHash,string role){using var c=f.Create(); return await c.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.users(organization_id,name,email,password_hash,role) VALUES (@organizationId,@name,@email,@passwordHash,@role) RETURNING id",new{organizationId,name,email,passwordHash,role});} public async Task TouchLoginAsync(Guid id){using var c=f.Create(); await c.ExecuteAsync("UPDATE valorapesquisa.users SET last_login_at=now() WHERE id=@id",new{id});}}
