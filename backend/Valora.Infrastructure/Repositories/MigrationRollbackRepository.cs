using System.Collections.Concurrent;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

internal static class MigrationImportStore
{ public static readonly ConcurrentDictionary<Guid,MigrationBatchDto> Batches=new(); public static readonly ConcurrentDictionary<Guid,MigrationSourceFileDto> Files=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRecordDto>> Records=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationConflictDto>> Conflicts=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationMappingDto>> Mappings=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRollbackItemDto>> Rollbacks=new(); }

public sealed class MigrationRollbackRepository : IMigrationRollbackRepository { public Task AddAsync(MigrationRollbackItemDto i,CancellationToken ct=default){const string sql="INSERT INTO valorapesquisa.migration_rollback_items(batch_id,target_entity,target_id,operation,before_json,after_json,status) VALUES (@BatchId,@TargetEntity,@TargetId,@Operation,CAST(@BeforeMaskedJson AS jsonb),CAST(@AfterMaskedJson AS jsonb),@Status)"; _=new CommandDefinition(sql,i,cancellationToken:ct); MigrationImportStore.Rollbacks.AddOrUpdate(i.BatchId,_=>new(){i},(_,l)=>{l.Add(i);return l;}); return Task.CompletedTask;} public Task<IReadOnlyList<MigrationRollbackItemDto>> ListByBatchAsync(Guid batchId,CancellationToken ct=default)=>Task.FromResult((IReadOnlyList<MigrationRollbackItemDto>)MigrationImportStore.Rollbacks.GetValueOrDefault(batchId,new()).ToList()); public Task MarkRolledBackAsync(Guid id,CancellationToken ct=default){foreach(var kv in MigrationImportStore.Rollbacks){var ix=kv.Value.FindIndex(x=>x.Id==id); if(ix>=0) kv.Value[ix]=kv.Value[ix] with{Status="rolled_back",RolledBackAt=DateTime.UtcNow};} return Task.CompletedTask;} }
