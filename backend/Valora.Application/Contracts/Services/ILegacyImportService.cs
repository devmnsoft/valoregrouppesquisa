using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ILegacyImportService { Task<MigrationValidationReportDto> DryRunAsync(MigrationDryRunRequest request,CancellationToken ct=default); }
