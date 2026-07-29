namespace Valora.Application.DTOs;

public sealed record EntitlementCheckResult(bool Allowed,string? Code,string Message);
