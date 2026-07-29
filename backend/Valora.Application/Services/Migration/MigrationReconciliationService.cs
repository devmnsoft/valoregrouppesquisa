using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class MigrationReconciliationService(
    IMigrationRecordRepository records,
    IMigrationConflictRepository conflicts,
    IAuditRepository audit) : IMigrationReconciliationService
{
    public async Task<MigrationReconciliationReportDto> ReconcileAsync(Guid batchId, CancellationToken ct = default)
    {
        var r = await records.ListByBatchAsync(batchId, ct);
        var c = await conflicts.ListByBatchAsync(batchId, ct);
        await audit.AddAsync(new AuditEntry(
            null,
            null,
            "migration.reconciliation.executed",
            "migration_batch",
            batchId.ToString(),
            "Conciliação executada",
            "{}"));

        return new MigrationReconciliationReportDto(
            batchId,
            c.Any(x => x.Severity == "blocking") ? "blocked" : "ready_with_warnings",
            r.GroupBy(x => x.TargetEntity).ToDictionary(g => g.Key, g => g.Count()),
            r.Where(x => x.Status != "invalid").GroupBy(x => x.TargetEntity).ToDictionary(g => g.Key, g => g.Count()),
            c.Select(x => $"{x.TargetEntity}:{x.ConflictType}").ToArray());
    }
}
