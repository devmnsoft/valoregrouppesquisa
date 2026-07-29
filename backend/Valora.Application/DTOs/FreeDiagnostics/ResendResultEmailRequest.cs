namespace Valora.Application.DTOs.FreeDiagnostics;

public sealed record ResendResultEmailRequest(string? Justification,bool Force=false);
