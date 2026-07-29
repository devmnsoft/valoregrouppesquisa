using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IExportService { Task<ExportJobDto> RequestAsync(Guid organizationId,Guid? userId,ExportRequest request); Task<IReadOnlyList<ExportJobDto>> ListAsync(Guid organizationId); Task<ExportJobDto?> GetAsync(Guid organizationId,Guid id); }
