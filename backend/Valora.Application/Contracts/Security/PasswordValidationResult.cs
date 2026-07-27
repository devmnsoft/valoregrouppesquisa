namespace Valora.Application.Contracts;

public sealed record PasswordValidationResult(bool IsValid, IReadOnlyList<string> Errors);
