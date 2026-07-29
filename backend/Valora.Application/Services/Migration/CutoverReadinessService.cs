using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

public sealed class CutoverReadinessService(
    IMigrationConflictRepository conflicts,
    IAuditRepository audit) : ICutoverReadinessService
{
    public async Task<CutoverReadinessDto> GetAsync(Guid batchId, CancellationToken ct = default)
    {
        var c = await conflicts.ListByBatchAsync(batchId, ct);
        var blockers = c
            .Where(x => x.Severity == "blocking")
            .Select(x => $"{x.TargetEntity}:{x.ConflictType}")
            .ToArray();
        await audit.AddAsync(new AuditEntry(
            null,
            null,
            "migration.cutover_readiness.generated",
            "migration_batch",
            batchId.ToString(),
            "Readiness de cutover gerado",
            "{}"));

        return new CutoverReadinessDto(
            batchId,
            blockers.Length > 0 ? "blocked" : "ready_with_warnings",
            new[] { "Dry-run executado", "Conciliação revisada", "Rollback planejado", "Auditoria ativa" },
            blockers,
            c.Where(x => x.Severity != "blocking").Select(x => x.ConflictType).ToArray(),
            "Executar janela manual, congelar legado, aplicar batch e validar amostras.",
            "Executar rollback por batch e reativar legado preservado.");
    }
}
