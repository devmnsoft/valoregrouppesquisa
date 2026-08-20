extern alias ValoraWeb;

using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using WebProgram = ValoraWeb::Program;
using NavigationCatalog = ValoraWeb::Valora.Web.Navigation.NavigationCatalog;
using NavigationDestination = ValoraWeb::Valora.Web.Navigation.NavigationDestination;
using NavigationService = ValoraWeb::Valora.Web.Navigation.NavigationService;
using INavigationRouteResolver = ValoraWeb::Valora.Web.Navigation.INavigationRouteResolver;
using BffAuthenticationService = ValoraWeb::Valora.Web.Services.Bff.BffAuthenticationService;
using ValoraIconRegistry = ValoraWeb::Valora.Web.Ui.ValoraIconRegistry;
using ValoraIconTagHelper = ValoraWeb::Valora.Web.Ui.ValoraIconTagHelper;

namespace Valora.Tests;

public sealed class NavigationRegressionTests
{
    [Fact]
    public void EveryCatalogItemUsesARegisteredIcon()
    {
        var registry = new ValoraIconRegistry();
        var missing = new NavigationCatalog().Sections
            .SelectMany(section => section.Items)
            .Where(item => !registry.Contains(item.Icon))
            .Select(item => $"{item.Code}: {item.Icon}")
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void SparklesIsAnOfficialDesignSystemIcon()
    {
        var registry = new ValoraIconRegistry();

        Assert.Contains("sparkles", registry.KnownIcons, StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(registry.GetRequired("sparkles"));
    }

    [Fact]
    public void UnknownIconRendersSafeFallbackWithoutThrowing()
    {
        var helper = new ValoraIconTagHelper(
            new ValoraIconRegistry(),
            NullLogger<ValoraIconTagHelper>.Instance,
            new TestEnvironment())
        {
            Name = "not-in-the-catalog",
            Decorative = true
        };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "icon-test");
        var output = new TagHelperOutput("valora-icon", new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        var exception = Record.Exception(() => helper.Process(context, output));

        Assert.Null(exception);
        Assert.Equal("svg", output.TagName);
        Assert.Equal(ValoraIconRegistry.FallbackIcon, output.Attributes["data-valora-icon"].Value);
        Assert.Equal("true", output.Attributes["data-icon-fallback"].Value);
        Assert.NotEmpty(output.Content.GetContent());
    }

    [Fact]
    public void EveryCatalogDestinationHasARealMvcControllerAction()
    {
        var assembly = typeof(WebProgram).Assembly;
        var missing = new List<string>();
        foreach (var destination in new NavigationCatalog().Sections.SelectMany(section => section.Items).Select(item => item.Destination))
        {
            var controller = assembly.GetType($"Valora.Web.Controllers.{destination.Controller}Controller");
            var exists = controller?.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Any(method => method.Name.Equals(destination.Action, StringComparison.OrdinalIgnoreCase)
                    && typeof(IActionResult).IsAssignableFrom(method.ReturnType)) == true;
            if (!exists) missing.Add($"{destination.Controller}.{destination.Action}");
        }

        Assert.Empty(missing);
    }

    [Fact]
    public async Task AdminValoraReceivesTheCompleteNavigationWithoutTenantClaims()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "admin_valora")], "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.Request.Path = "/Dashboard";
        var authentication = new BffAuthenticationService(null!, null!, NullLogger<BffAuthenticationService>.Instance);
        var service = new NavigationService(new NavigationCatalog(), authentication, new TestRoutes(), new TestEnvironment());

        var model = await service.BuildAsync(context);

        Assert.True(model.Sections.Count >= 6);
        var codes = model.Sections.SelectMany(section => section.Items).Select(item => item.Code).ToArray();
        Assert.Contains("valora.overview", codes);
        Assert.Contains("diagnostics.surveys", codes);
        Assert.Contains("intelligence.results", codes);
        Assert.Contains("intelligence.certificates", codes);
        Assert.Contains("administration.access", codes);
        Assert.Contains("administration.settings", codes);
        Assert.Contains("administration.organizations", codes);
        Assert.Contains("administration.roles", codes);
        Assert.Contains("administration.permissions", codes);
        Assert.Contains("administration.modules", codes);

        var labels = model.Sections.Select(section => section.Label).ToArray();
        Assert.Contains("Administração", labels);
        Assert.Contains("Plataforma", labels);
    }

    private sealed class TestRoutes : INavigationRouteResolver
    {
        public string? Resolve(NavigationDestination destination) => $"/{destination.Controller}/{destination.Action}";
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Valora.Web";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

[Trait("Category", "BffIntegration")]
public sealed class NavigationRenderingTests : IClassFixture<WebApplicationFactory<WebProgram>>
{
    private readonly WebApplicationFactory<WebProgram> _factory;
    public NavigationRenderingTests(WebApplicationFactory<WebProgram> factory) => _factory = factory;

    [Fact]
    public async Task DashboardAndNavigationComponentRenderWithoutAnException()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/Dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-admin-sidebar", html);
        Assert.Contains("Vamos configurar seu espaço", html);
        Assert.DoesNotContain("NavigationContext vazio", html);
    }
}
