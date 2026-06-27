using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
using Valora.Application.Results;
namespace Valora.Application.Services;
public sealed class PublicResponseTransactionService(IDbConnectionFactory db,IResponseRepository responses,IResultRepository results,ICertificateRepository certificates,ICommunicationRepository communications,IAuditRepository audit,IResultTokenService tokens)
{
    public async Task<SubmitSurveyResponseResult> SaveAsync(SurveyPublicReadModel survey, SubmitSurveyResponseRequest request, IReadOnlyList<ScoredAnswer> scored, ValoraInsightResult calc, IReadOnlyList<DimensionScoreInput> dimensions)
    {
        using var connection = db.Create(); connection.Open(); using var tx = connection.BeginTransaction();
        try
        {
            var resultToken = tokens.CreateToken(); var tokenHash = tokens.HashToken(resultToken);
            var responseId = await responses.CreateResponseAsync(survey.OrganizationId,survey.Id,survey.FormId,Val(request.Participant,"name"),Val(request.Participant,"email"),Val(request.Participant,"phone"),tokenHash,connection,tx);
            await responses.AddAnswersAsync(responseId, scored, connection, tx);
            await results.SaveResultAsync(survey.OrganizationId,responseId,calc.TotalScore,calc.MaxScore,Percent(calc),calc.Level,"Radar calculado com respostas reais por dimensão.",calc.StrategicTruth,calc.Risk,calc.NextLevel,tx);
            await results.SaveDimensionScoresAsync(survey.OrganizationId,responseId,dimensions,tx);
            var code = $"VAL-{responseId:N}"[..14]; var name = Val(request.Participant,"name"); var email = Val(request.Participant,"email");
            await certificates.CreateMetadataAsync(survey.OrganizationId,responseId,code,name,"Valora Pulse™",survey.Title,calc.Level,tx);
            var emailStatus = await SaveCommunicationAsync(survey,request,responseId,email,tx);
            await audit.LogAsync(new AuditEntry(survey.OrganizationId,null,"public_survey.submit","response",responseId.ToString(),"Resposta pública real recebida e calculada pela API PostgreSQL."),tx);
            tx.Commit(); return new(true,responseId,resultToken,emailStatus,new CertificateMetadataDto(responseId,code,"metadata-ready",name,"Valora Pulse™",survey.Title,DateTime.UtcNow),MapResult(calc,dimensions));
        }
        catch { tx.Rollback(); throw; }
    }
    async Task<string> SaveCommunicationAsync(SurveyPublicReadModel survey,SubmitSurveyResponseRequest request,Guid responseId,string? email,System.Data.IDbTransaction tx)
    { var status=request.CommunicationConsent&&!string.IsNullOrWhiteSpace(email)?"pending":"cancelled"; if(status=="pending"){await communications.CreateEmailJobAsync(survey.OrganizationId,responseId,email!,status,tx); await communications.CreateCommunicationAsync(survey.OrganizationId,survey.Id,responseId,"email","result-ready",status,Mask(email),tx);} return status; }
    static ResultScoreDto MapResult(ValoraInsightResult calc,IReadOnlyList<DimensionScoreInput> dims) => new(calc.TotalScore,calc.MaxScore,Percent(calc),calc.Level,dims.OrderByDescending(x=>x.Score).FirstOrDefault()?.DimensionName,dims.OrderBy(x=>x.Score).FirstOrDefault()?.DimensionName,"Radar calculado com respostas reais por dimensão.",calc.StrategicTruth,calc.Risk,calc.NextLevel);
    static decimal Percent(ValoraInsightResult c) => c.MaxScore == 0 ? 0 : Math.Round(c.TotalScore / c.MaxScore * 100, 2);
    static string? Val(Dictionary<string,object>? d,string k)=>d!=null&&d.TryGetValue(k,out var v)?v?.ToString():null;
    static string Mask(string email){var p=email.Split('@');return p.Length==2?$"{p[0][0]}***@{p[1]}":"***";}
}
