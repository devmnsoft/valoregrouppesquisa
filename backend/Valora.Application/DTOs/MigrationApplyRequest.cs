namespace Valora.Application.DTOs;

public sealed record MigrationApplyRequest(Guid BatchId, bool ConfirmApply, string RequestedByRole, string? ConfirmationText);
