namespace Valora.Api.Models;

public sealed record ApiResponse(bool Ok, string? Message = null, object? Data = null);
