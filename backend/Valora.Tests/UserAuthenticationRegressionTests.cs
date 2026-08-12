namespace Valora.Tests;

public sealed class UserAuthenticationRegressionTests
{
    [Fact]
    public void AuthenticationQueryUsesMutableRowProjectionAndMapsRecordExplicitly()
    {
        var source = File.ReadAllText(RepositoryPaths.InfrastructureFile("Repositories", "UserRepository.cs"));

        Assert.Contains("private sealed class AuthUserRow", source);
        Assert.Contains("QuerySingleOrDefaultAsync<AuthUserRow>", source);
        Assert.Contains("new UserAuthenticationRecord(", source);
        Assert.Contains("row.RoleCodesCsv ?? string.Empty", source);
        Assert.DoesNotContain("QuerySingleOrDefaultAsync<UserAuthenticationRecord>", source);
    }

    [Fact]
    public void AuthenticationQueryOnlySelectsActiveRowsAndDoesNotProjectDeletedAt()
    {
        var source = File.ReadAllText(RepositoryPaths.InfrastructureFile("Repositories", "UserRepository.cs"));
        var authenticationQuery = source[source.IndexOf("public async Task<UserAuthenticationRecord?> GetByEmailAsync", StringComparison.Ordinal)
            ..source.IndexOf("public async Task<UserRecord?> GetAsync", StringComparison.Ordinal)];

        Assert.Contains("u.deleted_at IS NULL", authenticationQuery);
        Assert.DoesNotContain("AS DeletedAt", authenticationQuery);
        foreach (var alias in new[] { "Id", "OrganizationId", "Name", "Email", "PasswordHash", "Status", "Phone", "RoleCodesCsv" })
        {
            Assert.Contains($"AS {alias}", authenticationQuery);
        }
    }

    [Fact]
    public void LoginKeepsAuthenticationFailuresGenericAndLogsOnlyMaskedEmail()
    {
        var source = File.ReadAllText(RepositoryPaths.ApplicationFile("Services", "Auth", "AuthService.cs"));
        var login = source[source.IndexOf("public async Task<AuthenticationResult> LoginAsync", StringComparison.Ordinal)
            ..source.IndexOf("public async Task<AuthenticationResult> RefreshAsync", StringComparison.Ordinal)];

        Assert.Contains("LogSanitizer.MaskEmail(request.Email)", login);
        Assert.Contains("user not found", login);
        Assert.Contains("user inactive", login);
        Assert.Contains("password hash empty or incompatible", login);
        Assert.Contains("password hash could not be verified", login);
        Assert.Contains("invalid password", login);
        Assert.Equal(8, login.Split("Credenciais inválidas.", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("Password={", login);
        Assert.DoesNotContain("Hash={", login);
        Assert.DoesNotContain("Token={", login);
    }
}
