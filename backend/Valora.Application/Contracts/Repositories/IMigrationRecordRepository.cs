using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationRecordRepository { Task AddAsync(MigrationRecordDto record,CancellationToken ct=default); Task<IReadOnlyList<MigrationRecordDto>> ListByBatchAsync(Guid batchId,CancellationToken ct=default); }
