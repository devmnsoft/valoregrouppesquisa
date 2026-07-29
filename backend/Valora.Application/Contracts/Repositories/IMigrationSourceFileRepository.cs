using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IMigrationSourceFileRepository { Task<MigrationSourceFileDto> CreateAsync(Guid? batchId,string fileName,string? contentType,long sizeBytes,string sha256,string? storedPath,string status,CancellationToken ct=default); Task<IReadOnlyList<MigrationSourceFileDto>> ListAsync(CancellationToken ct=default); Task<MigrationSourceFileDto?> GetAsync(Guid id,CancellationToken ct=default); }
