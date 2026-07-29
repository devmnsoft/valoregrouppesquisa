namespace Valora.Application.DTOs;

public sealed record MigrationRollbackItemDto(Guid Id, Guid BatchId, string TargetEntity, Guid TargetId, string Operation, string? BeforeMaskedJson, string? AfterMaskedJson, string Status, DateTime? RolledBackAt);
