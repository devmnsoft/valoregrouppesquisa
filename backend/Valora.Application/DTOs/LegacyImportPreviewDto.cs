namespace Valora.Application.DTOs;

public sealed record LegacyImportPreviewDto(Guid BatchId, string SourceType, string SourceName, MigrationSummaryDto Summary);
