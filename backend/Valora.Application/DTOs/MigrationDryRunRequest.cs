namespace Valora.Application.DTOs;

public sealed record MigrationDryRunRequest(Guid BatchId, IReadOnlyList<MigrationUploadRequest> Sources, bool ConfirmDryRun = true);
