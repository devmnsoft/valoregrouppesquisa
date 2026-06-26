namespace Valora.Application.DTOs;
public sealed record PublicResultResponse(bool Ok, PublicResponseDto Response, PublicSurveyDto Survey, PublicCompanyDto Company, ResultScoreDto Result, IReadOnlyList<DimensionScoreDto> Dimensions, CertificateMetadataDto Certificate);
