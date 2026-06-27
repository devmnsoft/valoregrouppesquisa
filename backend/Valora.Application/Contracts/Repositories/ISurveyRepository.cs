using Valora.Application.DTOs;
using Valora.Application.ReadModels;
namespace Valora.Application.Contracts;
public interface ISurveyRepository { Task<SurveyPublicReadModel?> GetPublicByTokenAsync(string token); Task<SurveyPublicReadModel?> GetActivePublicSurveyAsync(Guid surveyId); Task<SurveyLinkReadModel?> GetPublicLinkAsync(Guid surveyId); Task<bool> ValidatePublicTokenAsync(Guid surveyId,string token); Task<Guid> SaveResponseAsync(Guid surveyId, SubmitResponseRequest request); Task<int> CountActiveSurveysAsync(Guid organizationId); Task<int> CountResponsesThisMonthAsync(Guid organizationId); }
