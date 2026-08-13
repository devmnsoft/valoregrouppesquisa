using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
using Valora.Application.Results;
using Valora.Application.Security;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Application.Services;

public sealed class PublicResponseTransactionService(IDbConnectionFactory db, IResponseRepository responses, IResultRepository results, ICertificateRepository certificates, ICommunicationRepository communications, IAuditRepository audit, IResultTokenService tokens, IOrganizationalIntelligencePipeline intelligencePipeline, ILogger<PublicResponseTransactionService> logger)
{
    public async Task<SubmitSurveyResponseResult> SaveAsync(SurveyPublicReadModel survey, SubmitSurveyResponseRequest request, IReadOnlyList<ScoredAnswer> scored, ValoraInsightResult calc, IReadOnlyList<DimensionScoreInput> dimensions)
    {
        using var connection = db.Create(); connection.Open(); using var transaction = connection.BeginTransaction();
        logger.LogInformation("Public response transaction started. SurveyId={SurveyId} OrganizationId={OrganizationId}", survey.Id, survey.OrganizationId);
        try
        {
            var token = tokens.CreateToken(); var tokenHash = tokens.HashToken(token);
            var responseId = await responses.CreateResponseAsync(survey.OrganizationId, survey.Id, survey.FormId, Val(request.Participant, "name"), Val(request.Participant, "email"), Val(request.Participant, "phone"), tokenHash, connection, transaction);
            logger.LogInformation("Public response created. SurveyId={SurveyId} OrganizationId={OrganizationId} ResponseId={ResponseId}", survey.Id, survey.OrganizationId, responseId);
            await SaveLgpdConsentAsync(survey, request, responseId, Val(request.Participant, "email"), connection, transaction);
            await responses.AddAnswersAsync(responseId, scored, connection, transaction);
            logger.LogInformation("Public response answers saved. SurveyId={SurveyId} ResponseId={ResponseId} AnswerCount={AnswerCount}", survey.Id, responseId, scored.Count);
            await results.SaveResultAsync(survey.OrganizationId, responseId, calc.TotalScore, calc.MaxScore, Percent(calc), calc.Level, "Radar calculado com respostas reais por dimensão.", calc.StrategicTruth, calc.Risk, calc.NextLevel, transaction);
            logger.LogInformation("Public response result_scores saved. SurveyId={SurveyId} ResponseId={ResponseId}", survey.Id, responseId);
            await results.SaveDimensionScoresAsync(survey.OrganizationId, responseId, dimensions, transaction);
            logger.LogInformation("Public response dimension_scores saved. SurveyId={SurveyId} ResponseId={ResponseId} DimensionCount={DimensionCount}", survey.Id, responseId, dimensions.Count);
            var code = $"VAL-{responseId:N}"[..14]; var name = Val(request.Participant, "name"); var email = Val(request.Participant, "email");
            await certificates.CreateMetadataAsync(survey.OrganizationId, responseId, code, name, "Valora Group", survey.Title, calc.Level, transaction);
            logger.LogInformation("Public response certificate metadata saved. SurveyId={SurveyId} ResponseId={ResponseId}", survey.Id, responseId);
            var emailStatus = await SaveCommunicationAsync(survey, request, responseId, email, transaction);
            logger.LogInformation("Public response email job {EmailStatus}. SurveyId={SurveyId} ResponseId={ResponseId} Email={Email}", emailStatus, survey.Id, responseId, LogSanitizer.MaskEmail(email));
            await audit.LogAsync(new AuditEntry(survey.OrganizationId, null, "public_survey.submit", "response", responseId.ToString(), "Resposta pública real recebida e calculada pela API PostgreSQL."), transaction);
            logger.LogInformation("Public response audit_log created. SurveyId={SurveyId} ResponseId={ResponseId}", survey.Id, responseId);
            transaction.Commit();
            logger.LogInformation("Public response transaction committed. SurveyId={SurveyId} OrganizationId={OrganizationId} ResponseId={ResponseId}", survey.Id, survey.OrganizationId, responseId);
            try
            {
                await intelligencePipeline.ProcessResponseAsync(new(survey.OrganizationId, survey.Id, responseId, survey.FormId), CancellationToken.None);
                logger.LogInformation("Organizational intelligence pipeline completed. SurveyId={SurveyId} ResponseId={ResponseId}", survey.Id, responseId);
            }
            catch (Exception pipelineError)
            {
                // The response is already durably committed. Preserve it and expose the
                // processing failure through operations instead of asking the respondent
                // to submit the same evidence twice.
                logger.LogError(pipelineError, "Organizational intelligence pipeline failed after response commit. SurveyId={SurveyId} ResponseId={ResponseId}", survey.Id, responseId);
            }
            return new(true, responseId, token, emailStatus, new CertificateMetadataDto(responseId, code, "metadata-ready", name, "Valora Group", survey.Title, DateTime.UtcNow), MapResult(calc, dimensions));
        }
        catch (Exception ex)
        {
            try { transaction.Rollback(); logger.LogWarning("Rollback executado na submissão pública. SurveyId={SurveyId} OrganizationId={OrganizationId}", survey.Id, survey.OrganizationId); }
            catch (Exception rollbackEx) { logger.LogError(rollbackEx, "Falha ao executar rollback da submissão pública. SurveyId={SurveyId} OrganizationId={OrganizationId}", survey.Id, survey.OrganizationId); }
            logger.LogError(ex, "Erro transacional na submissão pública. SurveyId={SurveyId} OrganizationId={OrganizationId}", survey.Id, survey.OrganizationId);
            throw;
        }
    }
    async Task<string> SaveCommunicationAsync(SurveyPublicReadModel survey, SubmitSurveyResponseRequest request, Guid responseId, string? email, System.Data.IDbTransaction tx)
    { var status = request.CommunicationConsent && !string.IsNullOrWhiteSpace(email) ? "pending" : "cancelled"; if (status == "pending") { await communications.CreateEmailJobAsync(survey.OrganizationId, responseId, email!, status, tx); await communications.CreateCommunicationAsync(survey.OrganizationId, survey.Id, responseId, "email", "result-ready", status, LogSanitizer.MaskEmail(email) ?? "***", tx); } return status; }
    static ResultScoreDto MapResult(ValoraInsightResult calc, IReadOnlyList<DimensionScoreInput> dims) => new(calc.TotalScore, calc.MaxScore, Percent(calc), calc.Level, dims.OrderByDescending(x => x.Score).FirstOrDefault()?.DimensionName, dims.OrderBy(x => x.Score).FirstOrDefault()?.DimensionName, "Radar calculado com respostas reais por dimensão.", calc.StrategicTruth, calc.Risk, calc.NextLevel);
    static decimal Percent(ValoraInsightResult c) => c.MaxScore == 0 ? 0 : Math.Round(c.TotalScore / c.MaxScore * 100, 2);
    static string? Val(Dictionary<string, object>? d, string k) => d != null && d.TryGetValue(k, out var v) ? v?.ToString() : null;

    static Task SaveLgpdConsentAsync(SurveyPublicReadModel survey, SubmitSurveyResponseRequest request, Guid responseId, string? email, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction)
    {
        var suppliedVersion = Val(request.Participant, "consentVersion");
        var version = string.IsNullOrWhiteSpace(suppliedVersion) || suppliedVersion.Length > 32 ? "8.0" : suppliedVersion.Trim();
        var emailHash = string.IsNullOrWhiteSpace(email)
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()))).ToLowerInvariant();
        const string consentText = "Autorizo o tratamento das respostas para gerar o diagnóstico, conforme a Política de Privacidade Valora.";
        return connection.ExecuteAsync("""
            INSERT INTO valorapesquisa.lgpd_consents
                (organization_id, survey_id, response_id, participant_email_hash, consent_text, consent_version, accepted, accepted_at)
            VALUES
                (@organizationId, @surveyId, @responseId, @emailHash, @consentText, @version, true, now())
            """, new { organizationId = survey.OrganizationId, surveyId = survey.Id, responseId, emailHash, consentText, version }, transaction);
    }
}
