using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class MigrationApplyService(
    IMigrationBatchRepository batches,
    IMigrationConflictRepository conflicts,
    IMigrationMappingRepository mappings,
    IMigrationRecordRepository records,
    IMigrationRollbackRepository rollbacks,
    IAuditRepository audit) : IMigrationApplyService
{
    public async Task<MigrationReconciliationReportDto> ApplyAsync(
        MigrationApplyRequest request,
        CancellationToken ct = default)
    {
        await audit.AddAsync(new AuditEntry(
            null,
            null,
            "migration.apply.started",
            "migration_batch",
            request.BatchId.ToString(),
            "Apply iniciado",
            "{}"));

        try
        {
            if (!request.ConfirmApply)
            {
                throw new InvalidOperationException("Importação real exige confirmApply=true.");
            }

            if (request.RequestedByRole != "admin_valora")
            {
                throw new UnauthorizedAccessException("Apenas admin_valora pode aplicar importação nesta sprint.");
            }

            var batch = await batches.GetAsync(request.BatchId, ct)
                ?? throw new InvalidOperationException("Batch não encontrado.");
            if (!batch.Status.Contains("dry_run") && batch.Status != "blocked")
            {
                throw new InvalidOperationException("Apply exige dry-run anterior.");
            }

            if (await conflicts.HasBlockingAsync(request.BatchId, ct))
            {
                throw new InvalidOperationException("Conflito bloqueante impede apply.");
            }

            var all = (await records.ListByBatchAsync(request.BatchId, ct))
                .Where(x => x.Status != "invalid")
                .ToList();
            var batchSize = Math.Max(
                1,
                int.TryParse(Environment.GetEnvironmentVariable("VALORA_MIGRATION_BATCH_SIZE"), out var b) ? b : 500);

            foreach (var chunk in all.Chunk(batchSize))
            {
                foreach (var r in chunk)
                {
                    ct.ThrowIfCancellationRequested();
                    var target = Guid.NewGuid();
                    await rollbacks.AddAsync(new MigrationRollbackItemDto(
                        Guid.NewGuid(),
                        request.BatchId,
                        r.TargetEntity,
                        target,
                        "insert",
                        null,
                        r.NormalizedMaskedJson,
                        "planned",
                        null), ct);
                    await mappings.AddAsync(new MigrationMappingDto(
                        Guid.NewGuid(),
                        request.BatchId,
                        r.LegacyCollection,
                        r.LegacyId ?? r.Id.ToString(),
                        r.TargetEntity,
                        target,
                        $"{r.LegacyCollection}:{r.LegacyId}"), ct);
                }
            }

            await batches.UpdateStatusAsync(
                request.BatchId,
                "applied",
                JsonSerializer.Serialize(new { sensitive = "masked", batchSize }),
                ct);
            await audit.AddAsync(new AuditEntry(
                null,
                null,
                "migration.apply.completed",
                "migration_batch",
                request.BatchId.ToString(),
                "Apply concluído",
                "{}"));

            return new MigrationReconciliationReportDto(
                request.BatchId,
                "ready_with_warnings",
                new Dictionary<string, int>(),
                new Dictionary<string, int> { { "records", all.Count } },
                Array.Empty<string>());
        }
        catch
        {
            await audit.AddAsync(new AuditEntry(
                null,
                null,
                "migration.apply.failed",
                "migration_batch",
                request.BatchId.ToString(),
                "Apply falhou",
                "{}"));
            throw;
        }
    }
}
