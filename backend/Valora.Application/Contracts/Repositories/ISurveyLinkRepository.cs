namespace Valora.Application.Contracts;
public interface ISurveyLinkRepository { Task<IReadOnlyList<Valora.Application.DTOs.SurveyLinkDto>> ListBySurveyAsync(Guid organizationId,Guid surveyId); Task<Guid> CreateAsync(Guid organizationId,Guid surveyId,string publicUrl,DateTimeOffset? expiresAt); Task SetStatusAsync(Guid organizationId,Guid linkId,string status); }
