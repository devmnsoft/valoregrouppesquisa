namespace Valora.Application.DTOs;

public sealed record MigrationRollbackRequest(Guid BatchId, bool ConfirmRollback, string RequestedByRole, string? Reason);
