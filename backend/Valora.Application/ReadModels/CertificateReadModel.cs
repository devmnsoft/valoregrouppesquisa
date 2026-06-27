namespace Valora.Application.ReadModels;
public sealed record CertificateReadModel(Guid ResponseId,string CertificateCode,string? Status,string? ParticipantName,string? IssuerName,string? SurveyName,DateTime? IssuedAt);
