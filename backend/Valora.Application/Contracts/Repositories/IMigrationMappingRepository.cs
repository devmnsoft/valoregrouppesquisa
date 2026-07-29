using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationMappingRepository { Task AddAsync(MigrationMappingDto mapping,CancellationToken ct=default); Task<IReadOnlyList<MigrationMappingDto>> ListByBatchAsync(Guid batchId,CancellationToken ct=default); }
