namespace Valora.Application.DTOs;

public sealed record MigrationValidationReportDto(Guid BatchId, string Status, MigrationSummaryDto Summary, IReadOnlyList<MigrationRecordDto> Records, IReadOnlyList<MigrationConflictDto> Conflicts);
