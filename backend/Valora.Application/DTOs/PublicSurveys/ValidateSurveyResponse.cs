namespace Valora.Application.DTOs;
public sealed record ValidateSurveyResponse(bool Ok, PublicSurveyDto Survey, PublicFormDto Form, PublicCompanyDto Company, object Lgpd);
