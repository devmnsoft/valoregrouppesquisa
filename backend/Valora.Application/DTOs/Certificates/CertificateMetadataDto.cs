namespace Valora.Application.DTOs;
public sealed record CertificateMetadataDto(Guid? ResponseId,string CertificateCode,string Status,string? ParticipantName,string? IssuerName,string? SurveyName,DateTime? IssuedAt);
