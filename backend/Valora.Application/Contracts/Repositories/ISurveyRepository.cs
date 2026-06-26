using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface ISurveyRepository { Task<dynamic?> GetPublicByTokenAsync(string token); Task<Guid> SaveResponseAsync(Guid surveyId, SubmitResponseRequest request); Task<dynamic?> GetResultAsync(Guid responseId); Task<int> CountActiveSurveysAsync(Guid organizationId); Task<int> CountResponsesThisMonthAsync(Guid organizationId); }
