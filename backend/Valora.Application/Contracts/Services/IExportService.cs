using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IExportService
{
    Task<ExportJobDto> RequestAsync(Guid organizationId, Guid? userId, ExportRequest request, string correlationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExportJobDto>> ListAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<ExportJobDto?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
}
