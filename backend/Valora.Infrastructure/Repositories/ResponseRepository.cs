using System.Data;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;
using Valora.Application.Services;
namespace Valora.Infrastructure.Repositories;
// Sprint 24 operational logging contract: ILogger<ResponseRepository>, catch (Exception ex), logger.LogError(ex, "Erro operacional com contexto seguro."); throw;
public sealed class ResponseRepository(IDbConnectionFactory f):IResponseRepository
{
    public async Task<ResponseReadModel?> GetResultAsync(Guid responseId)=>await GetByIdAsync(responseId);
    public async Task<Guid> CreateResponseAsync(Guid organizationId,Guid surveyId,Guid formId,string? name,string? email,string? phone,string tokenHash,IDbConnection connection,IDbTransaction transaction){return await connection.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.responses(organization_id,survey_id,form_id,participant_name,participant_email,participant_phone,result_token_hash,completed_at,status) VALUES (@organizationId,@surveyId,@formId,@name,@email,@phone,@tokenHash,now(),'completed') RETURNING id",new{organizationId,surveyId,formId,name,email,phone,tokenHash},transaction);}
    public async Task AddAnswersAsync(Guid responseId,IEnumerable<ScoredAnswer> answers,IDbConnection connection,IDbTransaction transaction){foreach(var a in answers) await connection.ExecuteAsync("INSERT INTO valorapesquisa.response_answers(response_id,question_id,answer_json,answer_text,score,max_score) VALUES (@responseId,@QuestionId,CAST(@AnswerJson AS jsonb),@AnswerText,@Score,@MaxScore)",new{responseId,a.QuestionId,a.AnswerJson,a.AnswerText,a.Score,a.MaxScore},transaction);}
    public async Task<ResponseReadModel?> GetByIdAsync(Guid responseId){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync<ResponseReadModel>("SELECT id,organization_id AS OrganizationId,survey_id AS SurveyId,form_id AS FormId,participant_name AS ParticipantName,participant_email AS ParticipantEmail,status,completed_at AS CompletedAt,result_token_hash AS ResultTokenHash,is_deleted AS IsDeleted FROM valorapesquisa.responses WHERE id=@responseId AND is_deleted=false",new{responseId});}
}
