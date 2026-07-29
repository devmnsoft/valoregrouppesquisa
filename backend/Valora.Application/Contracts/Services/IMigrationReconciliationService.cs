using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationReconciliationService { Task<MigrationReconciliationReportDto> ReconcileAsync(Guid batchId,CancellationToken ct=default); }
