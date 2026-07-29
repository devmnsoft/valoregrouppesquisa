namespace Valora.Application.DTOs;

public sealed record MigrationMappingDto(Guid Id, Guid BatchId, string LegacyCollection, string LegacyId, string TargetEntity, Guid TargetId, string MappingKey);
