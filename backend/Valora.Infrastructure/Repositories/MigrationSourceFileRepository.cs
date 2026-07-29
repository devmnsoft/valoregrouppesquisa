using System.Collections.Concurrent;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

internal static class MigrationImportStore
{ public static readonly ConcurrentDictionary<Guid,MigrationBatchDto> Batches=new(); public static readonly ConcurrentDictionary<Guid,MigrationSourceFileDto> Files=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRecordDto>> Records=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationConflictDto>> Conflicts=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationMappingDto>> Mappings=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRollbackItemDto>> Rollbacks=new(); }

public sealed class MigrationSourceFileRepository : IMigrationSourceFileRepository
{ public Task<MigrationSourceFileDto> CreateAsync(Guid? batchId,string fileName,string? contentType,long sizeBytes,string sha256,string? storedPath,string status,CancellationToken ct=default){var dto=new MigrationSourceFileDto(Guid.NewGuid(),batchId,fileName,contentType,sizeBytes,sha256,storedPath,status,DateTime.UtcNow); MigrationImportStore.Files[dto.Id]=dto; return Task.FromResult(dto);} public Task<IReadOnlyList<MigrationSourceFileDto>> ListAsync(CancellationToken ct=default)=>Task.FromResult((IReadOnlyList<MigrationSourceFileDto>)MigrationImportStore.Files.Values.ToList()); public Task<MigrationSourceFileDto?> GetAsync(Guid id,CancellationToken ct=default)=>Task.FromResult(MigrationImportStore.Files.GetValueOrDefault(id)); }
