namespace Valora.Application.DTOs;
public sealed record SubmitSurveyResponseResult(bool Ok, Guid ResponseId, string ResultToken, string EmailStatus, CertificateMetadataDto Certificate, ResultScoreDto Result);
