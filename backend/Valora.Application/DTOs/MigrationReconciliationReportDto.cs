namespace Valora.Application.DTOs;

public sealed record MigrationReconciliationReportDto(Guid BatchId, string Status, IReadOnlyDictionary<string,int> LegacyCounts, IReadOnlyDictionary<string,int> ImportedCounts, IReadOnlyList<string> Divergences);
