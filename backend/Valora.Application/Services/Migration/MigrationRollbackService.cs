using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class MigrationRollbackService(
    IMigrationRollbackRepository repo,
    IAuditRepository audit) : IMigrationRollbackService
{
    public Task<IReadOnlyList<MigrationRollbackItemDto>> GetReportAsync(
        Guid batchId,
        CancellationToken ct = default) => repo.ListByBatchAsync(batchId, ct);

    public async Task<MigrationReconciliationReportDto> RollbackAsync(
        MigrationRollbackRequest request,
        CancellationToken ct = default)
    {
        await audit.AddAsync(new AuditEntry(
            null,
            null,
            "migration.rollback.started",
            "migration_batch",
            request.BatchId.ToString(),
            "Rollback iniciado",
            "{}"));

        try
        {
            if (!request.ConfirmRollback)
            {
                throw new InvalidOperationException("Rollback exige confirmRollback=true.");
            }

            if (request.RequestedByRole != "admin_valora")
            {
                throw new UnauthorizedAccessException("Apenas admin_valora pode executar rollback.");
            }

            var items = await repo.ListByBatchAsync(request.BatchId, ct);
            foreach (var i in items)
            {
                await repo.MarkRolledBackAsync(i.Id, ct);
            }

            await audit.AddAsync(new AuditEntry(
                null,
                null,
                "migration.rollback.completed",
                "migration_batch",
                request.BatchId.ToString(),
                "Rollback concluído",
                "{}"));

            return new MigrationReconciliationReportDto(
                request.BatchId,
                "rolled_back",
                new Dictionary<string, int>(),
                new Dictionary<string, int> { { "rollback_items", items.Count } },
                Array.Empty<string>());
        }
        catch
        {
            await audit.AddAsync(new AuditEntry(
                null,
                null,
                "migration.rollback.failed",
                "migration_batch",
                request.BatchId.ToString(),
                "Rollback falhou",
                "{}"));
            throw;
        }
    }
}
