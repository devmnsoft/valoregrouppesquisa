using System.Collections.Concurrent;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

internal static class MigrationImportStore
{ public static readonly ConcurrentDictionary<Guid,MigrationBatchDto> Batches=new(); public static readonly ConcurrentDictionary<Guid,MigrationSourceFileDto> Files=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRecordDto>> Records=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationConflictDto>> Conflicts=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationMappingDto>> Mappings=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRollbackItemDto>> Rollbacks=new(); }

public sealed class MigrationRecordRepository : IMigrationRecordRepository { public Task AddAsync(MigrationRecordDto r,CancellationToken ct=default){const string sql="INSERT INTO valorapesquisa.migration_records(batch_id,legacy_collection,legacy_id,target_entity,action,status,input_json,normalized_json) VALUES (@BatchId,@LegacyCollection,@LegacyId,@TargetEntity,@Action,@Status,CAST(@InputMaskedJson AS jsonb),CAST(@NormalizedMaskedJson AS jsonb))"; _=new CommandDefinition(sql,r,cancellationToken:ct); MigrationImportStore.Records.AddOrUpdate(r.BatchId,_=>new(){r},(_,l)=>{l.Add(r);return l;}); return Task.CompletedTask;} public Task<IReadOnlyList<MigrationRecordDto>> ListByBatchAsync(Guid batchId,CancellationToken ct=default)=>Task.FromResult((IReadOnlyList<MigrationRecordDto>)MigrationImportStore.Records.GetValueOrDefault(batchId,new()).ToList()); }
