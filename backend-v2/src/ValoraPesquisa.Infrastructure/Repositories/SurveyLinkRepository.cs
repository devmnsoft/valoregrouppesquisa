using System.Text.Json;
using Dapper;
using ValoraPesquisa.Application.Contracts;
using ValoraPesquisa.Application.DTOs;
using ValoraPesquisa.Domain.Entities;
using ValoraPesquisa.Infrastructure.Database;

namespace ValoraPesquisa.Infrastructure.Repositories;
public sealed class SurveyLinkRepository(PostgresConnectionFactory db,ITokenHasher hasher):ISurveyLinkRepository{
 const string dto="id,survey_id SurveyId,organization_id OrganizationId,public_url PublicUrl,status,expires_at ExpiresAt,revoked_at RevokedAt,created_at CreatedAt,updated_at UpdatedAt";
 public async Task<IReadOnlyList<SurveyLinkDto>> ListAsync(Guid surveyId,CurrentUser u){using var c=db.Create(); var s=await c.QuerySingleAsync<Guid>("select organization_id from valorapesquisa.surveys where id=@surveyId",new{surveyId}); Scope.DemandOrg(u,s); return (await c.QueryAsync<SurveyLinkDto>($"select {dto} from valorapesquisa.survey_links where survey_id=@surveyId order by created_at desc",new{surveyId})).ToList();}
 public async Task<CreatedSurveyLinkDto> CreateAsync(SurveyLink l,string token){using var c=db.Create(); await c.ExecuteAsync("insert into valorapesquisa.survey_links(id,survey_id,organization_id,token_hash,public_url,status,expires_at,created_at) values(@Id,@SurveyId,@OrganizationId,@TokenHash,@PublicUrl,@Status,@ExpiresAt,@CreatedAt)",l); return new(l.Id,l.SurveyId,l.OrganizationId,l.PublicUrl,token,l.Status,l.ExpiresAt);}
 public async Task<SurveyLink?> FindValidAsync(Guid surveyId,string slug,string token){using var c=db.Create(); var hash=hasher.Hash(token); return await c.QuerySingleOrDefaultAsync<SurveyLink>("select l.* from valorapesquisa.survey_links l join valorapesquisa.organizations o on o.id=l.organization_id join valorapesquisa.surveys s on s.id=l.survey_id where l.survey_id=@surveyId and o.slug=@slug and l.token_hash=@hash and l.status='active' and l.revoked_at is null and (l.expires_at is null or l.expires_at>now()) and s.status='published' and o.is_deleted=false",new{surveyId,slug,hash});}
 public async Task SetStatusAsync(Guid id,string status,CurrentUser u){using var c=db.Create(); var org=await c.QuerySingleAsync<Guid>("select organization_id from valorapesquisa.survey_links where id=@id",new{id}); Scope.DemandOrg(u,org); await c.ExecuteAsync("update valorapesquisa.survey_links set status=@status,revoked_at=case when @status in ('revoked','inactive') then now() else revoked_at end,updated_at=now() where id=@id",new{id,status});}}
