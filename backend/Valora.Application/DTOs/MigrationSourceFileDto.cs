namespace Valora.Application.DTOs;

public sealed record MigrationSourceFileDto(Guid Id, Guid? BatchId, string FileName, string? ContentType, long SizeBytes, string Sha256, string? StoredPath, string Status, DateTime CreatedAt);
