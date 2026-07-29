using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationDryRunService { Task<MigrationValidationReportDto> ExecuteAsync(MigrationDryRunRequest request,CancellationToken ct=default); }
