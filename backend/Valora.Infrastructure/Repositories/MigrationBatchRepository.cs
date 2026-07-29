using System.Collections.Concurrent;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

internal static class MigrationImportStore
{ public static readonly ConcurrentDictionary<Guid,MigrationBatchDto> Batches=new(); public static readonly ConcurrentDictionary<Guid,MigrationSourceFileDto> Files=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRecordDto>> Records=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationConflictDto>> Conflicts=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationMappingDto>> Mappings=new(); public static readonly ConcurrentDictionary<Guid,List<MigrationRollbackItemDto>> Rollbacks=new(); }

public sealed class MigrationBatchRepository(IDbConnectionFactory f) : IMigrationBatchRepository
{ public Task<MigrationBatchDto> CreateAsync(string sourceType,string sourceName,string mode,string requestedBy,CancellationToken ct=default){const string sql="INSERT INTO valorapesquisa.migration_batches(source_type,source_name,mode,requested_by) VALUES (@sourceType,@sourceName,@mode,@requestedBy) RETURNING id"; _=new CommandDefinition(sql,new{sourceType,sourceName,mode,requestedBy},cancellationToken:ct); var dto=new MigrationBatchDto(Guid.NewGuid(),sourceType,sourceName,mode,"created",requestedBy,DateTime.UtcNow,null,0,0,0,0,0,0,0,"{}"); MigrationImportStore.Batches[dto.Id]=dto; return Task.FromResult(dto);} public Task<IReadOnlyList<MigrationBatchDto>> ListAsync(CancellationToken ct=default)=>Task.FromResult((IReadOnlyList<MigrationBatchDto>)MigrationImportStore.Batches.Values.ToList()); public Task<MigrationBatchDto?> GetAsync(Guid id,CancellationToken ct=default)=>Task.FromResult(MigrationImportStore.Batches.GetValueOrDefault(id)); public Task UpdateStatusAsync(Guid id,string status,string summaryMaskedJson,CancellationToken ct=default){const string sql="UPDATE valorapesquisa.migration_batches SET status=@status, summary_json=CAST(@summaryMaskedJson AS jsonb), updated_at=now() WHERE id=@id"; _=new CommandDefinition(sql,new{id,status,summaryMaskedJson},cancellationToken:ct); if(MigrationImportStore.Batches.TryGetValue(id,out var b)) MigrationImportStore.Batches[id]=b with{Status=status,SummaryMaskedJson=summaryMaskedJson,FinishedAt=DateTime.UtcNow}; return Task.CompletedTask;} }
