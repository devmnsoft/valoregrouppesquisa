using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IPublicSurveyService { Task<ValidateSurveyResponse> ValidateAsync(Guid surveyId, ValidateSurveyRequest request); Task<SubmitSurveyResponseResult> SubmitAsync(Guid surveyId, SubmitSurveyResponseRequest request); }
