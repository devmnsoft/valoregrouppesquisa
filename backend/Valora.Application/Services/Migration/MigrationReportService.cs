using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class MigrationReportService(
    IMigrationRecordRepository records,
    IMigrationConflictRepository conflicts,
    IMigrationReconciliationService rec) : IMigrationReportService
{
    public async Task<MigrationValidationReportDto> GetDryRunReportAsync(
        Guid batchId,
        CancellationToken ct = default)
    {
        var r = await records.ListByBatchAsync(batchId, ct);
        var c = await conflicts.ListByBatchAsync(batchId, ct);
        var s = new MigrationSummaryDto(
            r.Count,
            r.Count(x => x.Status != "invalid"),
            r.Count(x => x.Status == "invalid"),
            r.Count(x => x.Action == "insert"),
            r.Count(x => x.Action == "update"),
            r.Count(x => x.Action == "skip"),
            c.Count,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>());

        return new MigrationValidationReportDto(
            batchId,
            c.Any(x => x.Severity == "blocking") ? "blocked" : "dry_run_completed",
            s,
            r,
            c);
    }

    public Task<MigrationReconciliationReportDto> GetReconciliationAsync(
        Guid batchId,
        CancellationToken ct = default) => rec.ReconcileAsync(batchId, ct);
}
