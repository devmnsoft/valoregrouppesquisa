namespace Valora.Application.Migration;

public interface IMigrationRepository { Task<Guid> CreateBatchAsync(string source, CancellationToken cancellationToken = default); Task AppendLogAsync(Guid batchId, string entityType, string legacyId, string status, string? error, CancellationToken cancellationToken = default); }
