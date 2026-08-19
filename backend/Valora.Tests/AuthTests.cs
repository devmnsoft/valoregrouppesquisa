using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valora.Api.Configuration;

namespace Valora.Tests.Auth;

[Trait("Category", "Unit")]
public sealed class JwtConfigurationTests
{
    public static TheoryData<string?> InvalidKeys => new() { null, "", "   ", "fake-key-with-fewer-than-32" };

    [Theory]
    [MemberData(nameof(InvalidKeys))]
    public void AddJwtAuthentication_WhenSigningKeyIsInvalid_ReturnsSanitizedConfigurationError(string? signingKey)
    {
        var configuration = Configuration(signingKey);

        var error = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddJwtAuthentication(configuration));

        Assert.Contains("Jwt:SigningKey deve possuir pelo menos 32 caracteres", error.Message);
        if (!string.IsNullOrWhiteSpace(signingKey)) Assert.DoesNotContain(signingKey, error.Message);
    }

    [Fact]
    public void AddJwtAuthentication_WhenSigningKeyHas32Characters_RegistersAuthentication()
    {
        var services = new ServiceCollection();

        var returned = services.AddJwtAuthentication(Configuration(new string('x', 32)));

        Assert.Same(services, returned);
        Assert.Contains(services, descriptor => descriptor.ServiceType.FullName ==
            "Microsoft.AspNetCore.Authentication.IAuthenticationService");
    }

    private static IConfiguration Configuration(string? signingKey) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = signingKey,
            ["Jwt:Issuer"] = "Valora.Tests",
            ["Jwt:Audience"] = "Valora.Tests"
        }).Build();
}
