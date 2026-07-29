using System.Collections.Concurrent;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

internal static class MigrationImportStore
{ public static readonly ConcurrentDictionary<Guid,MigrationBatchDto> Batches=new(); public static readonly ConcurrentDictionary<Guid,MigrationSourceFileDto> Files=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRecordDto>> Records=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationConflictDto>> Conflicts=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationMappingDto>> Mappings=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRollbackItemDto>> Rollbacks=new(); }

public sealed class MigrationMappingRepository : IMigrationMappingRepository { public Task AddAsync(MigrationMappingDto m,CancellationToken ct=default){const string sql="INSERT INTO valorapesquisa.migration_mappings(batch_id,legacy_collection,legacy_id,target_entity,target_id,mapping_key) VALUES (@BatchId,@LegacyCollection,@LegacyId,@TargetEntity,@TargetId,@MappingKey)"; _=new CommandDefinition(sql,m,cancellationToken:ct); MigrationImportStore.Mappings.AddOrUpdate(m.BatchId,_=>new(){m},(_,l)=>{l.Add(m);return l;}); return Task.CompletedTask;} public Task<IReadOnlyList<MigrationMappingDto>> ListByBatchAsync(Guid batchId,CancellationToken ct=default)=>Task.FromResult((IReadOnlyList<MigrationMappingDto>)MigrationImportStore.Mappings.GetValueOrDefault(batchId,new()).ToList()); }
