using Valora.Application.Services;

namespace Valora.Tests;

[Trait("Category", "Unit")]
public sealed class PasswordPolicyTests
{
    private readonly PasswordPolicy policy = new();

    [Fact]
    public void AcceptsStrongPassword()
    {
        Assert.True(policy.Validate("Forte#2026Val", "admin@empresa.com", "Empresa").IsValid);
    }

    [Theory]
    [InlineData("Curta#1")]
    [InlineData("semmayuscula#123")]
    [InlineData("SEMMINUSCULA#123")]
    [InlineData("SemNumero####")]
    [InlineData("SemEspecial123")]
    [InlineData("senha12345!")]
    public void RejectsWeakPassword(string password)
    {
        Assert.False(policy.Validate(password).IsValid);
    }

    [Fact]
    public void RejectsIdentityFragments()
    {
        Assert.False(policy.Validate("Admin#Seguro2026", "admin@valora.com").IsValid);
        Assert.False(policy.Validate("Empresa#2026Aa", companyName: "Empresa").IsValid);
    }
}
