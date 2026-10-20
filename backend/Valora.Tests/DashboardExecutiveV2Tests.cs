extern alias ValoraWeb;

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using WebProgram = ValoraWeb::Program;

namespace Valora.Tests;

[Trait("Category", "BffIntegration")]
public sealed class DashboardExecutiveV2Tests : IClassFixture<WebApplicationFactory<WebProgram>> {
    private readonly WebApplicationFactory<WebProgram> _factory;

    public DashboardExecutiveV2Tests(WebApplicationFactory<WebProgram> factory) {
        _factory = factory;
    }

    private static string WebRootPath => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Valora.Web"));

    [Fact]
    public void Dashboard_View_Implements_Official_Executive_Blocks() {
        var viewPath = Path.Combine(WebRootPath, "Views", "Dashboard", "Index.cshtml");
        Assert.True(File.Exists(viewPath), $"Index.cshtml do Dashboard não encontrado em: {viewPath}");

        var content = File.ReadAllText(viewPath);

        // Bloco 1: Índice de Maturidade Organizacional™
        Assert.Contains("Índice de Maturidade Organizacional™", content);
        Assert.Contains("Métrica Metodológica Principal", content);

        // Bloco 2: Capacidades Organizacionais
        Assert.Contains("Capacidades Organizacionais Avaliadas", content);
        Assert.Contains("Liderança & Alinhamento", content);
        Assert.Contains("Comunicação & Clareza", content);
        Assert.Contains("Processos & Governança", content);

        // Bloco 3: Prioridades e Fatores de Atenção
        Assert.Contains("Fatores de Atenção & Prioridades", content);

        // Bloco 4: Valora Action™
        Assert.Contains("Valora Action™ — Compromissos & Planos Ativos", content);

        // Bloco 5: Insights IA™ Explicáveis e Modal Oficial
        Assert.Contains("Insights IA™ Homologados", content);
        Assert.Contains("Entenda esta análise", content);
        Assert.Contains("id=\"explainabilityModal\"", content);
        Assert.Contains("1. O que foi observado:", content);
        Assert.Contains("2. Evidências consideradas:", content);
        Assert.Contains("3. Grau de confiança & Limitações:", content);
        Assert.Contains("4. Próxima evolução recomendada:", content);
    }

    [Fact]
    public void Dashboard_Guarantees_Never_Fabricating_Zero_For_Missing_Evidence() {
        var viewPath = Path.Combine(WebRootPath, "Views", "Dashboard", "Index.cshtml");
        var content = File.ReadAllText(viewPath);

        // Explicit methodology guardrail
        Assert.Contains("nenhuma nota zero arbitrária é atribuída por ausência de dados", content);
    }

    [Fact]
    public async Task Dashboard_Endpoint_Requires_Authentication_And_Safely_Redirects() {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/Dashboard");
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found);
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/Account/Login", location, StringComparison.OrdinalIgnoreCase);
    }
}
