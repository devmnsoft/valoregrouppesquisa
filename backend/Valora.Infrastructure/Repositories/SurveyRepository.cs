using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
namespace Valora.Infrastructure.Repositories;
public sealed class SurveyRepository(IDbConnectionFactory f):ISurveyRepository
{
    public async Task<SurveyPublicReadModel?> GetPublicByTokenAsync(string token){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync<SurveyPublicReadModel>(BaseSql+" AND s.token_hash=@token",new{token});}
    public async Task<SurveyPublicReadModel?> GetActivePublicSurveyAsync(Guid surveyId){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync<SurveyPublicReadModel>(BaseSql+" AND s.id=@surveyId",new{surveyId});}
    public async Task<SurveyLinkReadModel?> GetPublicLinkAsync(Guid surveyId){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync<SurveyLinkReadModel>("SELECT id,survey_id AS SurveyId,public_url AS PublicUrl,status,starts_at AS StartsAt,expires_at AS ExpiresAt FROM valorapesquisa.survey_links WHERE survey_id=@surveyId AND is_deleted=false AND status='active' AND (starts_at IS NULL OR starts_at<=now()) AND (expires_at IS NULL OR expires_at>now()) ORDER BY created_at DESC LIMIT 1",new{surveyId});}
    public async Task<bool> ValidatePublicTokenAsync(Guid surveyId,string token){using var c=f.Create(); var ok=await c.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.survey_links WHERE survey_id=@surveyId AND token_hash=@token AND status='active' AND is_deleted=false AND (expires_at IS NULL OR expires_at>now())",new{surveyId,token}); if(ok>0)return true; return await c.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.surveys WHERE id=@surveyId AND token_hash=@token AND is_deleted=false",new{surveyId,token})>0;}
    public Task<Guid> SaveResponseAsync(Guid surveyId,SubmitResponseRequest request)=>throw new NotSupportedException("Use IPublicSurveyService.SubmitAsync for transactional public submission.");
    public async Task<int> CountActiveSurveysAsync(Guid organizationId){using var c=f.Create(); return await c.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.surveys WHERE organization_id=@organizationId AND status='active' AND is_deleted=false",new{organizationId});}
    public async Task<int> CountResponsesThisMonthAsync(Guid organizationId){using var c=f.Create(); return await c.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.responses WHERE organization_id=@organizationId AND created_at >= date_trunc('month', now()) AND is_deleted=false",new{organizationId});}
    const string BaseSql="SELECT s.id,s.organization_id AS OrganizationId,s.form_id AS FormId,s.title,s.description,s.status,s.lgpd_required AS LgpdRequired,o.name AS OrganizationName,o.public_name AS PublicName,o.slug AS OrganizationSlug,s.starts_at AS StartsAt,s.expires_at AS ExpiresAt FROM valorapesquisa.surveys s JOIN valorapesquisa.organizations o ON o.id=s.organization_id WHERE s.is_deleted=false AND s.status IN ('active','published','open') AND (s.starts_at IS NULL OR s.starts_at<=now()) AND (s.expires_at IS NULL OR s.expires_at>now())";
}
