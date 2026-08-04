extern alias ValoraWeb;

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WebProgram = ValoraWeb::Program;

namespace Valora.Tests;

[Trait("Category", "BffIntegration")]
public sealed class BffHostIntegrationTests : IClassFixture<WebApplicationFactory<WebProgram>>
{
    private readonly WebApplicationFactory<WebProgram> _factory;

    public BffHostIntegrationTests(WebApplicationFactory<WebProgram> factory) => _factory = factory;

    [Fact]
    public async Task HealthEndpoint_IsServedByTheRealWebPipeline()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/health/web");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Valora.Web", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
