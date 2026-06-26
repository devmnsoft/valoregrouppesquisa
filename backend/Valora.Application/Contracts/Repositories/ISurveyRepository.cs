using System.Data;
using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface ISurveyRepository { Task<dynamic?> GetPublicByTokenAsync(string token); Task<dynamic?> GetActivePublicSurveyAsync(Guid surveyId); Task<dynamic?> GetPublicLinkAsync(Guid surveyId); Task<bool> ValidatePublicTokenAsync(Guid surveyId,string token); Task<Guid> SaveResponseAsync(Guid surveyId, SubmitResponseRequest request); Task<int> CountActiveSurveysAsync(Guid organizationId); Task<int> CountResponsesThisMonthAsync(Guid organizationId); }
