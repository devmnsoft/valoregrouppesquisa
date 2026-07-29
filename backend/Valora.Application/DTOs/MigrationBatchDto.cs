namespace Valora.Application.DTOs;

public sealed record MigrationBatchDto(Guid Id, string SourceType, string SourceName, string Mode, string Status, string? RequestedBy, DateTime? StartedAt, DateTime? FinishedAt, int TotalRecords, int ValidRecords, int InvalidRecords, int ImportedRecords, int SkippedRecords, int ConflictRecords, int ErrorRecords, string SummaryMaskedJson);
