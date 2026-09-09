using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IExportRepository {
    Task<ExportJobDto> CreateAsync(Guid organizationId, Guid? requestedBy, string entity, string format,
        string? filterJson, string correlationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExportWorkItem>> ClaimAsync(string workerId, int take, CancellationToken cancellationToken);
    Task CompleteAsync(GeneratedExport export, CancellationToken cancellationToken);
    Task FailAsync(Guid id, string sanitizedError, DateTimeOffset? retryAt, bool deadLetter, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExportJobDto>> ListAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<ExportJobDto?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
}

public interface IExportDataReader {
    Task<ExportDataSet> ReadAsync(Guid organizationId, string entity, string filterJson, CancellationToken cancellationToken);
}
