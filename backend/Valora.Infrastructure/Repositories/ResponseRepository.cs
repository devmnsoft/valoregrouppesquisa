using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;
using Valora.Application.Security;
using Valora.Application.Services;
namespace Valora.Infrastructure.Repositories;

public sealed class ResponseRepository(IDbConnectionFactory f, ILogger<ResponseRepository> logger) : IResponseRepository {
    public async Task<ResponseReadModel?> GetResultAsync(Guid responseId) => await GetByIdAsync(responseId);
    public async Task<Guid> CreateResponseAsync(Guid organizationId, Guid surveyId, Guid formId, Guid formVersionId, string? name, string? email, string? phone, string tokenHash, IDbConnection connection, IDbTransaction transaction) { try { return await connection.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.responses(organization_id,survey_id,form_id,form_version_id,participant_name,participant_email,participant_phone,result_token_hash,completed_at,status) VALUES (@organizationId,@surveyId,@formId,@formVersionId,@name,@email,@phone,@tokenHash,now(),'completed') RETURNING id", new { organizationId, surveyId, formId, formVersionId, name, email, phone, tokenHash }, transaction); } catch (Exception ex) { logger.LogError(ex, "Erro ao criar resposta. OrganizationId={OrganizationId} SurveyId={SurveyId} FormId={FormId} FormVersionId={FormVersionId} Email={Email} Phone={Phone}", organizationId, surveyId, formId, formVersionId, LogSanitizer.MaskEmail(email), LogSanitizer.MaskPhone(phone)); throw; } }
    public async Task AddAnswersAsync(Guid responseId, IEnumerable<ScoredAnswer> answers, IDbConnection connection, IDbTransaction transaction) { try { foreach (var a in answers) await connection.ExecuteAsync("INSERT INTO valorapesquisa.response_answers(response_id,question_id,answer_json,answer_text,score,max_score) VALUES (@responseId,@QuestionId,CAST(@AnswerJson AS jsonb),@AnswerText,@Score,@MaxScore)", new { responseId, a.QuestionId, a.AnswerJson, a.AnswerText, a.Score, a.MaxScore }, transaction); } catch (Exception ex) { logger.LogError(ex, "Erro ao criar respostas normalizadas. ResponseId={ResponseId}", responseId); throw; } }
    public async Task<ResponseReadModel?> GetByIdAsync(Guid responseId) { try { using var c = f.Create(); return await c.QuerySingleOrDefaultAsync<ResponseReadModel>("SELECT id,organization_id AS OrganizationId,survey_id AS SurveyId,form_id AS FormId,form_version_id AS FormVersionId,participant_name AS ParticipantName,participant_email AS ParticipantEmail,status,completed_at AS CompletedAt,result_token_hash AS ResultTokenHash,is_deleted AS IsDeleted FROM valorapesquisa.responses WHERE id=@responseId AND is_deleted=false", new { responseId }); } catch (Exception ex) { logger.LogError(ex, "Erro ao buscar resposta. ResponseId={ResponseId}", responseId); throw; } }

    public async Task<IReadOnlyList<dynamic>> ListAdminAsync(Guid organizationId) { try { using var c = f.Create(); return (await c.QueryAsync("SELECT r.id,r.organization_id,r.survey_id,r.form_id,r.participant_name,r.participant_email,r.status,r.completed_at,r.created_at,s.title AS survey_title,f.name AS form_name FROM valorapesquisa.responses r LEFT JOIN valorapesquisa.surveys s ON s.id=r.survey_id LEFT JOIN valorapesquisa.forms f ON f.id=r.form_id WHERE r.organization_id=@organizationId AND r.is_deleted=false ORDER BY r.created_at DESC", new { organizationId })).ToList(); } catch (Exception ex) { logger.LogError(ex, "Erro ao listar respostas. OrganizationId={OrganizationId}", organizationId); throw; } }
    public async Task<AdminResultReadModel?> GetAdminAsync(Guid organizationId, Guid responseId, Guid? userId = null, bool canReadActionPlans = false, bool organizationWide = false) { try { using var c = f.Create(); const string sql="""
        SELECT r.id ResponseId,r.organization_id OrganizationId,coalesce(o.name,'Organização') OrganizationName,
               r.survey_id SurveyId,coalesce(s.title,s.name,'Diagnóstico') SurveyTitle,s.starts_at PeriodStart,s.expires_at PeriodEnd,
               r.form_id FormId,coalesce(f.name,'Formulário') FormName,r.form_version_id FormVersionId,fv.version FormVersion,
               rr.id ResultId,
               CASE WHEN r.status<>'completed' THEN 'awaiting_responses' WHEN rs.id IS NULL THEN 'processing_pending' ELSE 'available' END ProcessingStatus,
               rs.created_at ProcessedAt,r.completed_at CompletedAt,rs.total_score TotalScore,rs.max_score MaxScore,
               rs.percentage Percentage,rs.maturity_label MaturityLabel,rs.radar_text RadarText,
               rs.strategic_truth StrategicTruth,rs.risk_if_nothing_changes RiskIfNothingChanges,rs.next_level NextLevel,
               count(*) FILTER(WHERE eligible.status='completed')::int EligibleResponseCount
          FROM valorapesquisa.responses r
          JOIN valorapesquisa.organizations o ON o.id=r.organization_id
          JOIN valorapesquisa.surveys s ON s.id=r.survey_id AND s.organization_id=r.organization_id
          JOIN valorapesquisa.forms f ON f.id=r.form_id AND (f.organization_id=r.organization_id OR f.is_global=true OR f.organization_id IS NULL)
          JOIN valorapesquisa.form_versions fv ON fv.id=r.form_version_id AND fv.form_id=r.form_id
          LEFT JOIN valorapesquisa.result_scores rs ON rs.response_id=r.id AND rs.organization_id=r.organization_id
          LEFT JOIN valorapesquisa.results rr ON rr.response_id=r.id AND rr.organization_id=r.organization_id
          LEFT JOIN valorapesquisa.responses eligible ON eligible.survey_id=r.survey_id AND eligible.organization_id=r.organization_id AND eligible.is_deleted=false
         WHERE r.id=@responseId AND r.organization_id=@organizationId AND r.is_deleted=false
         GROUP BY r.id,o.name,s.id,f.id,fv.id,rr.id,rs.id;
        SELECT d.dimension_name DimensionName,d.score Score,d.max_score MaxScore,d.percentage Percentage,d.level_label LevelLabel
          FROM valorapesquisa.dimension_scores d JOIN valorapesquisa.responses r ON r.id=d.response_id AND r.organization_id=d.organization_id
         WHERE d.response_id=@responseId AND d.organization_id=@organizationId ORDER BY d.dimension_name;
        SELECT gr.id,gr.title,gr.format,gr.status,gr.file_name FileName,gr.mime_type MimeType,gr.created_at CreatedAt
          FROM valorapesquisa.generated_reports gr WHERE gr.organization_id=@organizationId AND gr.response_id=@responseId ORDER BY gr.created_at DESC;
        SELECT p.id,p.title,p.status,p.priority,u.name OwnerName,p.due_at DueAt,
               coalesce((SELECT round(avg(i.progress_percent))::int FROM valorapesquisa.action_items i WHERE i.action_plan_id=p.id AND i.organization_id=p.organization_id AND i.deleted_at IS NULL),0) ProgressPercent
          FROM valorapesquisa.action_plans p LEFT JOIN valorapesquisa.users u ON u.id=p.owner_user_id AND u.organization_id=p.organization_id
         JOIN valorapesquisa.results rr ON rr.id=p.result_id AND rr.organization_id=p.organization_id
         WHERE p.organization_id=@organizationId AND rr.response_id=@responseId AND p.deleted_at IS NULL
           AND @canReadActionPlans
           AND (@organizationWide OR p.owner_user_id IS NULL OR p.owner_user_id=@userId OR EXISTS(
               SELECT 1 FROM valorapesquisa.action_items visible_item
                WHERE visible_item.action_plan_id=p.id AND visible_item.organization_id=p.organization_id
                  AND visible_item.deleted_at IS NULL AND visible_item.responsible_user_id=@userId))
         ORDER BY p.created_at DESC;
        """; using var q=await c.QueryMultipleAsync(sql,new{organizationId,responseId,userId,canReadActionPlans,organizationWide});var head=await q.ReadSingleOrDefaultAsync<AdminResultHead>();if(head is null)return null;var dimensions=(await q.ReadAsync<AdminResultDimensionReadModel>()).AsList();var reports=(await q.ReadAsync<AdminResultReportReadModel>()).AsList();var plans=(await q.ReadAsync<AdminResultPlanReadModel>()).AsList();return new(head.ResponseId,head.OrganizationId,head.OrganizationName,head.SurveyId,head.SurveyTitle,head.PeriodStart,head.PeriodEnd,head.FormId,head.FormName,head.FormVersionId,head.FormVersion,head.ResultId,head.ProcessingStatus,head.ProcessedAt,head.CompletedAt,head.TotalScore,head.MaxScore,head.Percentage,head.MaturityLabel,head.RadarText,head.StrategicTruth,head.RiskIfNothingChanges,head.NextLevel,head.EligibleResponseCount,dimensions,reports,plans); } catch (Exception ex) { logger.LogError(ex, "Erro ao buscar resultado administrativo. OrganizationId={OrganizationId} ResponseId={ResponseId}", organizationId, responseId); throw; } }

    private sealed record AdminResultHead(Guid ResponseId,Guid OrganizationId,string OrganizationName,Guid SurveyId,string SurveyTitle,DateTimeOffset? PeriodStart,DateTimeOffset? PeriodEnd,Guid FormId,string FormName,Guid FormVersionId,int FormVersion,Guid? ResultId,string ProcessingStatus,DateTimeOffset? ProcessedAt,DateTimeOffset? CompletedAt,decimal? TotalScore,decimal? MaxScore,decimal? Percentage,string? MaturityLabel,string? RadarText,string? StrategicTruth,string? RiskIfNothingChanges,string? NextLevel,int EligibleResponseCount);

}
