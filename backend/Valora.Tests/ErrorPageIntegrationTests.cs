extern alias ValoraWeb;

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WebProgram = ValoraWeb::Program;

namespace Valora.Tests;

[Trait("Category", "BffIntegration")]
public sealed class ErrorPageIntegrationTests : IClassFixture<WebApplicationFactory<WebProgram>>
{
    private readonly WebApplicationFactory<WebProgram> _factory;

    public ErrorPageIntegrationTests(WebApplicationFactory<WebProgram> factory) => _factory = factory;

    [Theory]
    [InlineData(400, HttpStatusCode.BadRequest, "Não foi possível processar")]
    [InlineData(401, HttpStatusCode.Unauthorized, "Sua sessão expirou")]
    [InlineData(403, HttpStatusCode.Forbidden, "Acesso não autorizado")]
    [InlineData(404, HttpStatusCode.NotFound, "Página não encontrada")]
    [InlineData(500, HttpStatusCode.InternalServerError, "Não foi possível concluir")]
    public async Task FriendlyErrorPagePreservesItsHttpStatus(int code, HttpStatusCode expected, string heading)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync($"/error/{code}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(expected, response.StatusCode);
        Assert.Contains(heading, html, StringComparison.Ordinal);
        Assert.Contains("Referência para suporte", html, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Exception", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownRouteUsesTheFriendly404Page()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/rota-que-nao-existe-rc");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Página não encontrada", html, StringComparison.Ordinal);
    }
}
