using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationRollbackService { Task<MigrationReconciliationReportDto> RollbackAsync(MigrationRollbackRequest request,CancellationToken ct=default); Task<IReadOnlyList<MigrationRollbackItemDto>> GetReportAsync(Guid batchId,CancellationToken ct=default); }
