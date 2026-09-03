using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class BffSessionRegressionTests
{
    [Fact]
    public void AuthenticationCookieCarriesSafeOrganizationAndSessionContext()
    {
        var source = File.ReadAllText(RepositoryPaths.WebFile("Services", "Bff", "BffAuthenticationService.cs"));

        Assert.Contains("Guid.TryParse", File.ReadAllText(RepositoryPaths.WebFile("Services", "CurrentOrganizationProvider.cs")));
        Assert.Contains("new Claim(\"organization_id\"", source);
        Assert.Contains("new Claim(\"tenant_id\"", source);
        Assert.Contains("new Claim(\"session_id\"", source);
        Assert.Contains("AllowRefresh = true", source);
    }

    [Fact]
    public void FeatureUnauthorizedResponseDoesNotAutomaticallyDestroySession()
    {
        var source = File.ReadAllText(RepositoryPaths.WebFile("wwwroot", "js", "api", "ajax-client.js"));

        Assert.Contains("const isSessionFailure", source);
        Assert.Contains("if (isSessionFailure)", source);
        Assert.DoesNotContain("} else if (status === 401) {\n      clearToken();", source);
    }

    [Fact]
    public void RememberSessionChoiceReachesCookieAuthentication()
    {
        var page = File.ReadAllText(RepositoryPaths.WebFile("wwwroot", "js", "pages", "login-page.js"));
        var controller = File.ReadAllText(RepositoryPaths.WebFile("Controllers", "BffAuthController.cs"));

        Assert.Contains("rememberMe: form.rememberMe.checked", page);
        Assert.Contains("JsonValueKind.True", controller);
        Assert.Contains("rememberMe));", controller);
    }

    [Fact]
    public void RefreshPreservesPrimaryRoleAndTransientApiFailuresPreserveSession()
    {
        var source = File.ReadAllText(RepositoryPaths.WebFile("Services", "Bff", "BffAuthenticationService.cs"));

        Assert.Contains("new(ClaimTypes.Role, result.User.Role)", source);
        Assert.Contains("catch (BffApiUnavailableException exception)", source);
        Assert.Contains("return session;", source);
    }

    [Fact]
    public void CookieKeysArePersistentAndBffChallengesReturnJson()
    {
        var program = File.ReadAllText(RepositoryPaths.WebFile("Program.cs"));

        Assert.Contains("PersistKeysToFileSystem", program);
        Assert.Contains("Path.StartsWithSegments(\"/bff\")", program);
        Assert.Contains("application/problem+json", program);
        Assert.Contains("AUTHENTICATION_REQUIRED", program);
        Assert.Contains("options.FallbackPolicy", program);
        Assert.Contains("RequireAuthenticatedUser()", program);
    }
}
