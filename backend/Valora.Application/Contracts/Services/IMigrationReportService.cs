using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationReportService { Task<MigrationValidationReportDto> GetDryRunReportAsync(Guid batchId,CancellationToken ct=default); Task<MigrationReconciliationReportDto> GetReconciliationAsync(Guid batchId,CancellationToken ct=default); }
