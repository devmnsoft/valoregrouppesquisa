using System.Text.Json;
using Dapper;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Infrastructure.Database;

namespace ValoraPesquisa.Infrastructure.Repositories;
public sealed class UserRepository(PostgresConnectionFactory db):IUserRepository{
 const string dto="id,organization_id OrganizationId,name,email,role,status,phone,last_login_at LastLoginAt,created_at CreatedAt,updated_at UpdatedAt";
 public async Task<User?> FindByEmailAsync(string email){using var c=db.Create(); return await c.QuerySingleOrDefaultAsync<User>("select * from valorapesquisa.users where lower(email)=lower(@email) and is_deleted=false",new{email});}
 public async Task<User?> FindByIdAsync(Guid id){using var c=db.Create(); return await c.QuerySingleOrDefaultAsync<User>("select * from valorapesquisa.users where id=@id and is_deleted=false",new{id});}
 public async Task<IReadOnlyList<UserDto>> ListAsync(CurrentUser u){using var c=db.Create(); return (await c.QueryAsync<UserDto>(u.IsAdminValora?$"select {dto} from valorapesquisa.users where is_deleted=false order by name":$"select {dto} from valorapesquisa.users where organization_id=@OrganizationId and is_deleted=false order by name",u)).ToList();}
 public async Task<UserDto> CreateAsync(User u){using var c=db.Create(); await c.ExecuteAsync("insert into valorapesquisa.users(id,organization_id,name,email,password_hash,role,status,phone,created_at,is_deleted) values(@Id,@OrganizationId,@Name,@Email,@PasswordHash,@Role,@Status,@Phone,@CreatedAt,false)",u); return (await c.QuerySingleAsync<UserDto>($"select {dto} from valorapesquisa.users where id=@Id",u));}
 public async Task<UserDto> UpdateAsync(Guid id,string name,string email,string? phone,string role,Guid? organizationId,CurrentUser u){ if(!u.IsAdminValora && organizationId!=u.OrganizationId) throw new UnauthorizedAccessException("Acesso negado."); using var c=db.Create(); await c.ExecuteAsync("update valorapesquisa.users set name=@name,email=@email,phone=@phone,role=@role,organization_id=@organizationId,updated_at=now() where id=@id and is_deleted=false",new{id,name,email,phone,role,organizationId}); return await c.QuerySingleAsync<UserDto>($"select {dto} from valorapesquisa.users where id=@id",new{id});}
 public async Task SetStatusAsync(Guid id,string status,CurrentUser u){ var target=await FindByIdAsync(id)??throw new ArgumentException("Usuário não encontrado."); if(target.OrganizationId.HasValue)Scope.DemandOrg(u,target.OrganizationId.Value); using var c=db.Create(); await c.ExecuteAsync("update valorapesquisa.users set status=@status,updated_at=now() where id=@id",new{id,status});}
 public async Task UpdateLastLoginAsync(Guid id){using var c=db.Create(); await c.ExecuteAsync("update valorapesquisa.users set last_login_at=now() where id=@id",new{id});}}
