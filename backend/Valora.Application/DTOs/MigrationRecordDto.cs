namespace Valora.Application.DTOs;

public sealed record MigrationRecordDto(Guid Id, Guid BatchId, Guid? SourceFileId, string LegacyCollection, string? LegacyId, string TargetEntity, Guid? TargetId, string Action, string Status, string InputMaskedJson, string NormalizedMaskedJson, string? ErrorCode, string? ErrorMessage);
