using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationBatchRepository { Task<MigrationBatchDto> CreateAsync(string sourceType,string sourceName,string mode,string requestedBy,CancellationToken ct=default); Task<IReadOnlyList<MigrationBatchDto>> ListAsync(CancellationToken ct=default); Task<MigrationBatchDto?> GetAsync(Guid id,CancellationToken ct=default); Task UpdateStatusAsync(Guid id,string status,string summaryMaskedJson,CancellationToken ct=default); }
