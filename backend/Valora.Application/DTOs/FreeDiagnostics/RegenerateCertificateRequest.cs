namespace Valora.Application.DTOs.FreeDiagnostics;

public sealed record RegenerateCertificateRequest(string? Justification,string? LayoutVersion=null);
