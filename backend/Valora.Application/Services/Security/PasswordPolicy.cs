using Valora.Application.Contracts;

namespace Valora.Application.Services;

public sealed class PasswordPolicy : IPasswordPolicy
{
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "1234567890", "password123!", "senha12345!", "qwerty12345!", "admin12345!"
    };

    public PasswordValidationResult Validate(string password, string? email = null, string? companyName = null)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password) || password.Length < 10) errors.Add("A senha deve possuir ao menos 10 caracteres.");
        if (!password.Any(char.IsUpper)) errors.Add("A senha deve possuir letra maiúscula.");
        if (!password.Any(char.IsLower)) errors.Add("A senha deve possuir letra minúscula.");
        if (!password.Any(char.IsDigit)) errors.Add("A senha deve possuir número.");
        if (!password.Any(character => !char.IsLetterOrDigit(character))) errors.Add("A senha deve possuir caractere especial.");
        if (CommonPasswords.Contains(password)) errors.Add("A senha informada não é permitida.");

        var emailLocalPart = email?.Split('@', 2)[0];
        if (!string.IsNullOrWhiteSpace(emailLocalPart) && emailLocalPart.Length >= 3 && password.Contains(emailLocalPart, StringComparison.OrdinalIgnoreCase))
            errors.Add("A senha não pode conter o e-mail.");
        if (!string.IsNullOrWhiteSpace(companyName) && companyName.Length >= 3 && password.Contains(companyName.Trim(), StringComparison.OrdinalIgnoreCase))
            errors.Add("A senha não pode conter o nome da empresa.");

        return new PasswordValidationResult(errors.Count == 0, errors);
    }
}
