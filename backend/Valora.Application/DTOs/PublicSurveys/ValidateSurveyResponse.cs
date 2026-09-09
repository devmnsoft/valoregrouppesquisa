namespace Valora.Application.DTOs;

public sealed record PublicLgpdDto(bool Required);
public sealed record ValidateSurveyResponse(bool Ok, PublicSurveyDto Survey, PublicFormDto Form, PublicCompanyDto Company, PublicLgpdDto Lgpd);
