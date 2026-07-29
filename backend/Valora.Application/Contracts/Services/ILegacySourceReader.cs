using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface ILegacySourceReader { bool CanRead(string sourceType); Task<LegacySourceReadResult> ReadAsync(MigrationUploadRequest request,CancellationToken ct=default); }
