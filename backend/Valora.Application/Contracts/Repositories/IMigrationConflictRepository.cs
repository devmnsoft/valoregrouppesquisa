using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationConflictRepository { Task AddAsync(MigrationConflictDto conflict,CancellationToken ct=default); Task<IReadOnlyList<MigrationConflictDto>> ListByBatchAsync(Guid batchId,CancellationToken ct=default); Task ResolveAsync(Guid conflictId,string resolution,string resolvedBy,CancellationToken ct=default); Task<bool> HasBlockingAsync(Guid batchId,CancellationToken ct=default); }
