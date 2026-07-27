namespace Valora.Application.Contracts;

public interface IPasswordPolicy
{
    PasswordValidationResult Validate(string password, string? email = null, string? companyName = null);
}
