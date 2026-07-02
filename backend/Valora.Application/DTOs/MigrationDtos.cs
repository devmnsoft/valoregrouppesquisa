namespace Valora.Application.DTOs;

public sealed record MigrationBatchDto(Guid Id, string SourceType, string SourceName, string Mode, string Status, string? RequestedBy, DateTime? StartedAt, DateTime? FinishedAt, int TotalRecords, int ValidRecords, int InvalidRecords, int ImportedRecords, int SkippedRecords, int ConflictRecords, int ErrorRecords, string SummaryMaskedJson);
public sealed record MigrationSourceFileDto(Guid Id, Guid? BatchId, string FileName, string? ContentType, long SizeBytes, string Sha256, string? StoredPath, string Status, DateTime CreatedAt);
public sealed record MigrationRecordDto(Guid Id, Guid BatchId, Guid? SourceFileId, string LegacyCollection, string? LegacyId, string TargetEntity, Guid? TargetId, string Action, string Status, string InputMaskedJson, string NormalizedMaskedJson, string? ErrorCode, string? ErrorMessage);
public sealed record MigrationConflictDto(Guid Id, Guid BatchId, string LegacyCollection, string? LegacyId, string TargetEntity, Guid? TargetId, string ConflictType, string Severity, string LegacyValueMaskedJson, string CurrentValueMaskedJson, string? Resolution, string? ResolvedBy, DateTime? ResolvedAt);
public sealed record MigrationMappingDto(Guid Id, Guid BatchId, string LegacyCollection, string LegacyId, string TargetEntity, Guid TargetId, string MappingKey);
public sealed record MigrationRollbackItemDto(Guid Id, Guid BatchId, string TargetEntity, Guid TargetId, string Operation, string? BeforeMaskedJson, string? AfterMaskedJson, string Status, DateTime? RolledBackAt);
public sealed record MigrationDryRunRequest(Guid BatchId, IReadOnlyList<MigrationUploadRequest> Sources, bool ConfirmDryRun = true);
public sealed record MigrationApplyRequest(Guid BatchId, bool ConfirmApply, string RequestedByRole, string? ConfirmationText);
public sealed record MigrationRollbackRequest(Guid BatchId, bool ConfirmRollback, string RequestedByRole, string? Reason);
public sealed record MigrationUploadRequest(string SourceType, string SourceName, string FileName, string ContentType, string PayloadJson);
public sealed record MigrationSummaryDto(int TotalRead, int Valid, int Invalid, int WouldInsert, int WouldUpdate, int WouldSkip, int Conflicts, IReadOnlyList<string> UnmappedFields, IReadOnlyList<string> SensitiveDataDetected, IReadOnlyList<string> Risks);
public sealed record MigrationValidationReportDto(Guid BatchId, string Status, MigrationSummaryDto Summary, IReadOnlyList<MigrationRecordDto> Records, IReadOnlyList<MigrationConflictDto> Conflicts);
public sealed record MigrationReconciliationReportDto(Guid BatchId, string Status, IReadOnlyDictionary<string,int> LegacyCounts, IReadOnlyDictionary<string,int> ImportedCounts, IReadOnlyList<string> Divergences);
public sealed record LegacyImportPreviewDto(Guid BatchId, string SourceType, string SourceName, MigrationSummaryDto Summary);
public sealed record LegacyImportDiffDto(string Entity, string LegacyId, string Field, string LegacyMaskedValue, string CurrentMaskedValue, string Severity);
public sealed record CutoverReadinessDto(Guid BatchId, string Status, IReadOnlyList<string> Checklist, IReadOnlyList<string> Blockers, IReadOnlyList<string> Warnings, string ManualCutoverPlan, string RollbackPlan);
