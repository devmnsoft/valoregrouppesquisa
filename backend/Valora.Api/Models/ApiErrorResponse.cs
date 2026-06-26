namespace Valora.Api.Models;

public sealed record ApiErrorResponse(bool Ok, string Message, string Code);
