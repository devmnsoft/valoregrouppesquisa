namespace Valora.Application.DTOs.FreeDiagnostics;

public sealed record FreeDiagnosticActionResult(bool Ok,string Status,string Message,string? CorrelationId=null);
