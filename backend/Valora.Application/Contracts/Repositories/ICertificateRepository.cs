using System.Data;
using Valora.Application.ReadModels;
namespace Valora.Application.Contracts;
public interface ICertificateRepository { Task<CertificateReadModel?> GetByResponseAsync(Guid responseId); Task CreateMetadataAsync(Guid responseId,string validationCode,string status); Task CreateMetadataAsync(Guid organizationId,Guid responseId,string code,string? participantName,string issuerName,string surveyName,string maturityLabel,IDbTransaction transaction); }
