using System.Text.Json;
using Dapper;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Infrastructure.Database;

namespace ValoraPesquisa.Infrastructure.Repositories;
public sealed class SurveyRepository(PostgresConnectionFactory db,IFormRepository forms):ISurveyRepository{
 const string dto="id,organization_id OrganizationId,form_id FormId,title,description,status,starts_at StartsAt,expires_at ExpiresAt,show_result ShowResult,allow_repeat AllowRepeat,created_at CreatedAt,updated_at UpdatedAt";
 public async Task<IReadOnlyList<SurveyDto>> ListAsync(CurrentUser u){using var c=db.Create();return(await c.QueryAsync<SurveyDto>(u.IsAdminValora?$"select {dto} from valorapesquisa.surveys where is_deleted=false order by created_at desc":$"select {dto} from valorapesquisa.surveys where organization_id=@OrganizationId and is_deleted=false order by created_at desc",u)).ToList();}
 public async Task<SurveyDto?> GetAsync(Guid id,CurrentUser? u=null){using var c=db.Create();var s=await c.QuerySingleOrDefaultAsync<SurveyDto>($"select {dto} from valorapesquisa.surveys where id=@id and is_deleted=false",new{id}); if(s!=null&&u!=null)Scope.DemandOrg(u,s.OrganizationId); return s;}
 public async Task<Survey?> GetEntityAsync(Guid id){using var c=db.Create(); return await c.QuerySingleOrDefaultAsync<Survey>("select * from valorapesquisa.surveys where id=@id and is_deleted=false",new{id});}
 public async Task<SurveyDto> SaveAsync(Survey s,CurrentUser u){Scope.DemandOrg(u,s.OrganizationId); if(!await forms.HasQuestionsAsync(s.FormId,s.OrganizationId))throw new ArgumentException("Formulário inválido ou sem perguntas."); using var c=db.Create(); await c.ExecuteAsync("insert into valorapesquisa.surveys(id,organization_id,form_id,title,description,status,starts_at,expires_at,show_result,allow_repeat,created_at,is_deleted) values(@Id,@OrganizationId,@FormId,@Title,@Description,@Status,@StartsAt,@ExpiresAt,@ShowResult,@AllowRepeat,@CreatedAt,false) on conflict(id) do update set title=@Title,description=@Description,status=@Status,starts_at=@StartsAt,expires_at=@ExpiresAt,show_result=@ShowResult,allow_repeat=@AllowRepeat,updated_at=now()",s); return (await GetAsync(s.Id,u))!;}
 public async Task SetStatusAsync(Guid id,string status,CurrentUser u){var s=await GetAsync(id,u)??throw new ArgumentException("Pesquisa não encontrada."); if(status=="published"&&!await forms.HasQuestionsAsync(s.FormId,s.OrganizationId))throw new ArgumentException("Só é possível publicar formulário com perguntas."); using var c=db.Create(); await c.ExecuteAsync("update valorapesquisa.surveys set status=@status,updated_at=now() where id=@id",new{id,status});}}
