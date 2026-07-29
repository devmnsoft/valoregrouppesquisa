namespace Valora.Application.DTOs;

public sealed record MigrationConflictDto(Guid Id, Guid BatchId, string LegacyCollection, string? LegacyId, string TargetEntity, Guid? TargetId, string ConflictType, string Severity, string LegacyValueMaskedJson, string CurrentValueMaskedJson, string? Resolution, string? ResolvedBy, DateTime? ResolvedAt);
