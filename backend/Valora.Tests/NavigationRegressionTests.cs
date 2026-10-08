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
using BffAuthenticationService = ValoraWeb::Valora.Web.Services.Bff.BffAuthenticationService;
using INavigationRouteResolver = ValoraWeb::Valora.Web.Navigation.INavigationRouteResolver;
using NavigationCatalog = ValoraWeb::Valora.Web.Navigation.NavigationCatalog;
using NavigationDestination = ValoraWeb::Valora.Web.Navigation.NavigationDestination;
using NavigationService = ValoraWeb::Valora.Web.Navigation.NavigationService;
using ValoraIconRegistry = ValoraWeb::Valora.Web.Ui.ValoraIconRegistry;
using ValoraIconTagHelper = ValoraWeb::Valora.Web.Ui.ValoraIconTagHelper;
using WebProgram = ValoraWeb::Program;

namespace Valora.Tests;

public sealed class NavigationRegressionTests {
    [Fact]
    public void EveryCatalogItemUsesARegisteredIcon() {
        var registry = new ValoraIconRegistry();
        var missing = new NavigationCatalog().Sections
            .SelectMany(section => section.Items)
            .Where(item => !registry.Contains(item.Icon))
            .Select(item => $"{item.Code}: {item.Icon}")
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void Catalog_does_not_publish_the_same_destination_as_different_modules() {
        var duplicates = new NavigationCatalog().Sections.SelectMany(x => x.Items)
            .GroupBy(x => $"{x.Destination.Controller}/{x.Destination.Action}", StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        Assert.Empty(duplicates);
    }

    [Fact]
    public void Catalog_publishes_the_ActionCenter_plans_destination_exactly_once_and_never_alias_controllers() {
        var items = new NavigationCatalog().Sections.SelectMany(x => x.Items).ToArray();
        var aliases = items.Where(x => x.Destination.Controller is "ActionPlans" or "ValoraAction").Select(x => x.Code).ToArray();

        Assert.Empty(aliases);
        Assert.Single(items, x => x.Destination.Controller.Equals("ActionCenter", StringComparison.OrdinalIgnoreCase)
            && x.Destination.Action.Equals("Plans", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SparklesIsAnOfficialDesignSystemIcon() {
        var registry = new ValoraIconRegistry();

        Assert.Contains("sparkles", registry.KnownIcons, StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(registry.GetRequired("sparkles"));
    }

    [Fact]
    public void UnknownIconRendersSafeFallbackWithoutThrowing() {
        var helper = new ValoraIconTagHelper(
            new ValoraIconRegistry(),
            NullLogger<ValoraIconTagHelper>.Instance,
            new TestEnvironment()) {
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
    public void EveryCatalogDestinationHasARealMvcControllerAction() {
        var assembly = typeof(WebProgram).Assembly;
        var missing = new List<string>();
        foreach (var destination in new NavigationCatalog().Sections.SelectMany(section => section.Items).Select(item => item.Destination)) {
            var controller = assembly.GetType($"Valora.Web.Controllers.{destination.Controller}Controller");
            var exists = controller?.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Any(method => method.Name.Equals(destination.Action, StringComparison.OrdinalIgnoreCase)
                    && IsActionResult(method.ReturnType)) == true;
            if (!exists) missing.Add($"{destination.Controller}.{destination.Action}");
        }

        Assert.Empty(missing);
    }

    private static bool IsActionResult(Type returnType) =>
        typeof(IActionResult).IsAssignableFrom(returnType)
        || (returnType.IsGenericType
            && returnType.GetGenericTypeDefinition() == typeof(Task<>)
            && typeof(IActionResult).IsAssignableFrom(returnType.GenericTypeArguments[0]));

    [Fact]
    public async Task AdminValoraReceivesTheCompleteNavigationWithoutTenantClaims() {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "admin_valora")], "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.Request.Path = "/Dashboard";
        var authentication = new BffAuthenticationService(null!, null!, NullLogger<BffAuthenticationService>.Instance);
        var service = new NavigationService(new NavigationCatalog(), authentication, new TestRoutes(), new TestEnvironment());

        var model = await service.BuildAsync(context);

        // Bloco B — 8 seções canônicas: 7 áreas do cliente + ADMINISTRAÇÃO GLOBAL.
        var labels = model.Sections.Select(section => section.Label).ToArray();
        Assert.Equal(8, labels.Length);
        foreach (var expected in new[] { "Visão Executiva", "Diagnóstico", "Resultados", "Inteligência", "Execução & Evolução", "Risco & Governança", "Organização & Segurança", "ADMINISTRAÇÃO GLOBAL" }) {
            Assert.Contains(expected, labels);
        }

        var codes = model.Sections.SelectMany(section => section.Items).Select(item => item.Code).ToArray();
        foreach (var required in new[] { "valora.overview", "diagnostics.surveys", "intelligence.results", "intelligence.certificates", "administration.settings", "administration.roles", "administration.permissions", "saas.modules", "saas.customers", "execution.plans", "security.overview", "methodology.overview" }) {
            Assert.Contains(required, codes);
        }
        foreach (var removed in new[] { "administration.organizations", "master.organizations", "intelligence.evidence-center", "subscriptions.current" }) {
            Assert.DoesNotContain(removed, codes);
        }
    }

    // Bloco B — aliases GET legados redirecionam para as rotas canônicas.
    [Fact]
    public void LegacyAliasActionsRedirectToTheirCanonicalRoutes() {
        var centers = new ValoraWeb::Valora.Web.Controllers.OrganizationalCentersController();
        Assert.Equal("/Workspace/Priorities", ((RedirectResult)centers.Priorities()).Url);
        Assert.Equal("/Intelligence/Evidence", ((RedirectResult)centers.Evidence()).Url);
        Assert.Equal("/Intelligence/Indices", ((RedirectResult)centers.Indexes()).Url);
        Assert.Equal("/Intelligence/Radar", ((RedirectResult)centers.Radar()).Url);

        var administration = new ValoraWeb::Valora.Web.Controllers.AdministrationController();
        Assert.Equal("/AdminValora?module=companies", ((RedirectResult)administration.Organizations()).Url);

        var saas = new ValoraWeb::Valora.Web.Controllers.SaasController(null!, NullLogger<ValoraWeb::Valora.Web.Controllers.SaasController>.Instance);
        Assert.Equal("/Marketplace", ((RedirectResult)saas.Subscription()).Url);
    }

    private sealed class TestRoutes : INavigationRouteResolver {
        public string? Resolve(NavigationDestination destination) => $"/{destination.Controller}/{destination.Action}";
    }

    private sealed class TestEnvironment : IWebHostEnvironment {
        public string ApplicationName { get; set; } = "Valora.Web";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

[Trait("Category", "BffIntegration")]
public sealed class NavigationRenderingTests : IClassFixture<WebApplicationFactory<WebProgram>> {
    private readonly WebApplicationFactory<WebProgram> _factory;
    public NavigationRenderingTests(WebApplicationFactory<WebProgram> factory) => _factory = factory;

    [Fact]
    public async Task DashboardAndNavigationComponentRenderWithoutAnException() {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/Dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
        Assert.Empty(html);
    }
}
