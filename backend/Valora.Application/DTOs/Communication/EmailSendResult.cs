namespace Valora.Application.DTOs.Communication;
public sealed record EmailSendResult(bool Ok, Guid? JobId, string Status, string Message, string? CorrelationId = null);
