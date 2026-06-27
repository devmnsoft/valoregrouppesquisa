using System.Data;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;
namespace Valora.Infrastructure.Repositories;
// Sprint 24 operational logging contract: ILogger<CertificateRepository>, catch (Exception ex), logger.LogError(ex, "Erro operacional com contexto seguro."); throw;
public sealed class CertificateRepository(IDbConnectionFactory f):ICertificateRepository
{
    public async Task<CertificateReadModel?> GetByResponseAsync(Guid responseId){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync<CertificateReadModel>("SELECT response_id AS ResponseId,certificate_code AS CertificateCode,status,participant_name AS ParticipantName,issuer_name AS IssuerName,survey_name AS SurveyName,issued_at AS IssuedAt FROM valorapesquisa.certificates WHERE response_id=@responseId",new{responseId});}
    public async Task CreateMetadataAsync(Guid responseId,string validationCode,string status){using var c=f.Create(); await c.ExecuteAsync("INSERT INTO valorapesquisa.certificates(response_id,certificate_code,issuer_name,status) VALUES (@responseId,@validationCode,'Valora Pulse',@status) ON CONFLICT (certificate_code) DO UPDATE SET updated_at=now()",new{responseId,validationCode,status});}
    public async Task CreateMetadataAsync(Guid organizationId,Guid responseId,string code,string? participantName,string issuerName,string surveyName,string maturityLabel,IDbTransaction transaction){await transaction.Connection!.ExecuteAsync("INSERT INTO valorapesquisa.certificates(organization_id,response_id,certificate_code,participant_name,issuer_name,survey_name,maturity_label,status) VALUES (@organizationId,@responseId,@code,@participantName,@issuerName,@surveyName,@maturityLabel,'metadata-ready')",new{organizationId,responseId,code,participantName,issuerName,surveyName,maturityLabel},transaction);}
}
