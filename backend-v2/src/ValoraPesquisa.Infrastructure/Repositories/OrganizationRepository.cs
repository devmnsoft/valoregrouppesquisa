using System.Text.Json;
using Dapper;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Infrastructure.Database;

namespace ValoraPesquisa.Infrastructure.Repositories;
public sealed class OrganizationRepository(PostgresConnectionFactory db):IOrganizationRepository{
 const string cols="id,name,public_name PublicName,slug,document,email,phone,status,plan_code PlanCode,created_at CreatedAt,updated_at UpdatedAt";
 public async Task<IReadOnlyList<OrganizationDto>> ListAsync(CurrentUser u){ using var c=db.Create(); var sql=u.IsAdminValora?$"select {cols} from valorapesquisa.organizations where is_deleted=false order by name":$"select {cols} from valorapesquisa.organizations where id=@OrganizationId and is_deleted=false"; return (await c.QueryAsync<OrganizationDto>(sql,u)).ToList(); }
 public async Task<OrganizationDto?> FindByIdAsync(Guid id,CurrentUser? u=null){ if(u!=null)Scope.DemandOrg(u,id); using var c=db.Create(); return await c.QuerySingleOrDefaultAsync<OrganizationDto>($"select {cols} from valorapesquisa.organizations where id=@id and is_deleted=false",new{id}); }
 public async Task<Organization?> FindEntityByIdAsync(Guid id){ using var c=db.Create(); return await c.QuerySingleOrDefaultAsync<Organization>("select * from valorapesquisa.organizations where id=@id and is_deleted=false",new{id}); }
 public async Task<OrganizationDto?> FindBySlugAsync(string slug){ using var c=db.Create(); return await c.QuerySingleOrDefaultAsync<OrganizationDto>($"select {cols} from valorapesquisa.organizations where slug=@slug and is_deleted=false",new{slug}); }
 public async Task<OrganizationDto> CreateAsync(Organization o){ using var c=db.Create(); await c.ExecuteAsync("insert into valorapesquisa.organizations(id,name,public_name,slug,document,email,phone,status,plan_code,created_at,created_by,updated_by,is_deleted) values(@Id,@Name,@PublicName,@Slug,@Document,@Email,@Phone,@Status,@PlanCode,@CreatedAt,@CreatedBy,@UpdatedBy,false)",o); return (await FindByIdAsync(o.Id))!; }
 public async Task<OrganizationDto> UpdateAsync(Guid id,string name,string publicName,string slug,string? document,string email,string? phone,string status,string planCode,CurrentUser u){ Scope.DemandOrg(u,id); using var c=db.Create(); await c.ExecuteAsync("update valorapesquisa.organizations set name=@name,public_name=@publicName,slug=@slug,document=@document,email=@email,phone=@phone,status=@status,plan_code=@planCode,updated_at=now(),updated_by=@UserId where id=@id and is_deleted=false",new{id,name,publicName,slug,document,email,phone,status,planCode,UserId=u.Id}); return (await FindByIdAsync(id,u))!; }}
