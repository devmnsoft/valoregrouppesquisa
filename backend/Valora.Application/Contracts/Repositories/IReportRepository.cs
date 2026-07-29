using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IReportRepository { Task<IReadOnlyList<ReportDefinitionDto>> ListDefinitionsAsync(); Task<GeneratedReportDto> CreateGeneratedAsync(Guid organizationId,Guid? surveyId,Guid? responseId,Guid? definitionId,string title,string format,string payloadJson,Guid? generatedBy); Task<IReadOnlyList<GeneratedReportDto>> ListGeneratedAsync(Guid organizationId); Task<GeneratedReportDto?> GetGeneratedAsync(Guid organizationId,Guid id); }
