using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Valora.Tests;

[Trait("Category", "ApiIntegration")]
public sealed class ApiHostIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiHostIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task RootEndpoint_IsServedByTheRealApiPipeline()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Valora.Api", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
