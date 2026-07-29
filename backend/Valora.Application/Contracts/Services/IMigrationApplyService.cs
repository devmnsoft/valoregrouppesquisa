using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationApplyService { Task<MigrationReconciliationReportDto> ApplyAsync(MigrationApplyRequest request,CancellationToken ct=default); }
