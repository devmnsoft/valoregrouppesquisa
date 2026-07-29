using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationRollbackRepository { Task AddAsync(MigrationRollbackItemDto item,CancellationToken ct=default); Task<IReadOnlyList<MigrationRollbackItemDto>> ListByBatchAsync(Guid batchId,CancellationToken ct=default); Task MarkRolledBackAsync(Guid id,CancellationToken ct=default); }
