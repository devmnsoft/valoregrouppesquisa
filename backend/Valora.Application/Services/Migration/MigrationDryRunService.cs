using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class MigrationDryRunService(
    IEnumerable<ILegacySourceReader> readers,
    IMigrationRecordRepository records,
    IMigrationConflictRepository conflicts,
    IMigrationBatchRepository batches,
    IAuditRepository audit) : IMigrationDryRunService, ILegacyImportService
{
    public Task<MigrationValidationReportDto> DryRunAsync(
        MigrationDryRunRequest request,
        CancellationToken ct = default) => ExecuteAsync(request, ct);

    public async Task<MigrationValidationReportDto> ExecuteAsync(
        MigrationDryRunRequest request,
        CancellationToken ct = default)
    {
        await audit.AddAsync(new AuditEntry(
            null,
            null,
            "migration.dry_run.started",
            "migration_batch",
            request.BatchId.ToString(),
            "Dry-run iniciado",
            "{}"));

        var started = DateTime.UtcNow;
        var recs = new List<MigrationRecordDto>();
        var cons = new List<MigrationConflictDto>();
        var unmapped = new HashSet<string>();
        var sensitive = new HashSet<string>();
        var total = 0;
        var invalid = 0;

        try
        {
            foreach (var src in request.Sources)
            {
                ct.ThrowIfCancellationRequested();
                var reader = readers.FirstOrDefault(r => r.CanRead(src.SourceType))
                    ?? throw new InvalidOperationException("Fonte de importação não suportada.");
                var data = await reader.ReadAsync(src, ct);

                foreach (var d in data.Documents)
                {
                    ct.ThrowIfCancellationRequested();
                    total++;

                    foreach (var u in d.UnmappedFields)
                    {
                        unmapped.Add($"{d.Collection}.{u}");
                    }

                    foreach (var s in d.SensitiveFields)
                    {
                        sensitive.Add($"{d.Collection}.{s}");
                    }

                    var status = d.TargetEntity == "manual_review" ? "invalid" : "planned";
                    if (status == "invalid")
                    {
                        invalid++;
                    }

                    var dto = new MigrationRecordDto(
                        Guid.NewGuid(),
                        request.BatchId,
                        null,
                        d.Collection,
                        d.LegacyId,
                        d.TargetEntity,
                        null,
                        "insert",
                        status,
                        d.MaskedJson,
                        d.NormalizedMaskedJson,
                        status == "invalid" ? "UNMAPPED_COLLECTION" : null,
                        status == "invalid" ? "Coleção sem destino oficial claro." : null);

                    recs.Add(dto);
                    await records.AddAsync(dto, ct);

                    if (status == "invalid")
                    {
                        var c = new MigrationConflictDto(
                            Guid.NewGuid(),
                            request.BatchId,
                            d.Collection,
                            d.LegacyId,
                            d.TargetEntity,
                            null,
                            "unmapped_collection",
                            "blocking",
                            d.MaskedJson,
                            "{}",
                            null,
                            null,
                            null);

                        cons.Add(c);
                        await conflicts.AddAsync(c, ct);
                        await audit.AddAsync(new AuditEntry(
                            null,
                            null,
                            "migration.conflict.registered",
                            "migration_conflict",
                            c.Id.ToString(),
                            "Conflito registrado sem payload sensível",
                            "{}"));
                    }
                }
            }

            var statusFinal = cons.Any(c => c.Severity == "blocking") ? "blocked" : "dry_run_completed";
            var sum = new MigrationSummaryDto(
                total,
                total - invalid,
                invalid,
                total - invalid,
                0,
                0,
                cons.Count,
                unmapped.ToArray(),
                sensitive.ToArray(),
                cons.Count > 0 ? new[] { "Há conflitos bloqueantes antes do apply." } : Array.Empty<string>());

            await batches.UpdateStatusAsync(
                request.BatchId,
                statusFinal,
                JsonSerializer.Serialize(new
                {
                    total,
                    batchSize = Environment.GetEnvironmentVariable("VALORA_MIGRATION_BATCH_SIZE") ?? "500",
                    elapsedMs = (long)(DateTime.UtcNow - started).TotalMilliseconds
                }),
                ct);
            await audit.AddAsync(new AuditEntry(
                null,
                null,
                "migration.dry_run.completed",
                "migration_batch",
                request.BatchId.ToString(),
                "Dry-run concluído",
                "{}"));

            return new MigrationValidationReportDto(request.BatchId, statusFinal, sum, recs, cons);
        }
        catch
        {
            await audit.AddAsync(new AuditEntry(
                null,
                null,
                "migration.dry_run.failed",
                "migration_batch",
                request.BatchId.ToString(),
                "Dry-run falhou",
                "{}"));
            throw;
        }
    }
}
