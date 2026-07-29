using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IReportService { Task<GeneratedReportDto> GenerateSurveyAsync(Guid organizationId,Guid surveyId,string format,Guid? userId); Task<GeneratedReportDto> GenerateResponseAsync(Guid organizationId,Guid responseId,string format,Guid? userId); Task<GeneratedReportDto> GenerateOrganizationAsync(Guid organizationId,string format,Guid? userId); Task<IReadOnlyList<GeneratedReportDto>> ListGeneratedAsync(Guid organizationId); Task<GeneratedReportDto?> GetGeneratedAsync(Guid organizationId,Guid id); }
