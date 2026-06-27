using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;
namespace Valora.Infrastructure.Repositories;
public sealed class CertificateRepository(IDbConnectionFactory f, ILogger<CertificateRepository> logger):ICertificateRepository
{
 public async Task<CertificateReadModel?> GetByResponseAsync(Guid responseId){try{using var c=f.Create(); return await c.QuerySingleOrDefaultAsync<CertificateReadModel>("SELECT response_id AS ResponseId,certificate_code AS CertificateCode,status,participant_name AS ParticipantName,issuer_name AS IssuerName,survey_name AS SurveyName,issued_at AS IssuedAt FROM valorapesquisa.certificates WHERE response_id=@responseId",new{responseId});}catch(Exception ex){logger.LogError(ex,"Erro ao buscar certificado. ResponseId={ResponseId}",responseId);throw;}}
 public async Task CreateMetadataAsync(Guid responseId,string validationCode,string status){try{using var c=f.Create(); await c.ExecuteAsync("INSERT INTO valorapesquisa.certificates(response_id,certificate_code,issuer_name,status) VALUES (@responseId,@validationCode,'Valora Pulse',@status) ON CONFLICT (certificate_code) DO UPDATE SET updated_at=now()",new{responseId,validationCode,status});}catch(Exception ex){logger.LogError(ex,"Erro ao criar metadados de certificado. ResponseId={ResponseId} Status={Status}",responseId,status);throw;}}
 public async Task CreateMetadataAsync(Guid organizationId,Guid responseId,string code,string? participantName,string issuerName,string surveyName,string maturityLabel,IDbTransaction transaction){try{await transaction.Connection!.ExecuteAsync("INSERT INTO valorapesquisa.certificates(organization_id,response_id,certificate_code,participant_name,issuer_name,survey_name,maturity_label,status) VALUES (@organizationId,@responseId,@code,@participantName,@issuerName,@surveyName,@maturityLabel,'metadata-ready')",new{organizationId,responseId,code,participantName,issuerName,surveyName,maturityLabel},transaction);}catch(Exception ex){logger.LogError(ex,"Erro ao criar metadados transacionais de certificado. OrganizationId={OrganizationId} ResponseId={ResponseId}",organizationId,responseId);throw;}}
}
