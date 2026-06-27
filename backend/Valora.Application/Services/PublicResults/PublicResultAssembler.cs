using Valora.Application.DTOs;
using Valora.Application.ReadModels;
namespace Valora.Application.Services;
public sealed class PublicResultAssembler
{
    public PublicResultResponse Assemble(ResponseReadModel r,SurveyPublicReadModel s,ResultScoreReadModel score,IReadOnlyList<DimensionScoreReadModel> dims,CertificateReadModel cert)
    {
        return new(true,new PublicResponseDto(r.Id,r.ParticipantName,r.ParticipantEmail,r.Status,r.CompletedAt),new PublicSurveyDto(s.Id,s.Title,s.Description,s.Status,s.LgpdRequired),new PublicCompanyDto(s.OrganizationId,s.OrganizationName,s.PublicName,s.OrganizationSlug),new ResultScoreDto(score.TotalScore,score.MaxScore,score.Percentage,score.MaturityLabel,null,null,score.RadarText,score.StrategicTruth,score.RiskIfNothingChanges,score.NextLevel),dims.Select(d=>new DimensionScoreDto(d.DimensionName,d.Score,d.MaxScore,d.Percentage,d.LevelLabel)).ToList(),new CertificateMetadataDto(cert.ResponseId,cert.CertificateCode,cert.Status??"metadata-ready",cert.ParticipantName,cert.IssuerName,cert.SurveyName,cert.IssuedAt));
    }
}
