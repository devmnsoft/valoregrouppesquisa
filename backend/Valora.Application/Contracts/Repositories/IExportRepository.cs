using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IExportRepository { Task<ExportJobDto> CreateAsync(Guid organizationId,Guid? requestedBy,string entity,string format,string? filterJson); Task CompleteAsync(Guid organizationId,Guid id,string fileName,string mimeType,string payload); Task<IReadOnlyList<ExportJobDto>> ListAsync(Guid organizationId); Task<ExportJobDto?> GetAsync(Guid organizationId,Guid id); }
