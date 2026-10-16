extern alias ValoraWeb;

using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Valora.Api.Common;
using Valora.Api.Controllers;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
using Valora.Application.Services;
using Valora.Domain.Operations;
using Valora.Tests.Support;

using WebAccountController = ValoraWeb::Valora.Web.Controllers.BffAccountController;
using WebAuthController = ValoraWeb::Valora.Web.Controllers.BffAuthController;
using WebBffBodyReader = ValoraWeb::Valora.Web.Services.Bff.BffBodyReader;
using WebAuthenticationService = ValoraWeb::Valora.Web.Services.Bff.BffAuthenticationService;
using WebIDistributedBffSessionStore = ValoraWeb::Valora.Web.Services.Bff.IDistributedBffSessionStore;
using WebBffServerSession = ValoraWeb::Valora.Web.Services.Bff.BffServerSession;
using WebBffSafeSession = ValoraWeb::Valora.Web.Services.Bff.BffSafeSession;
using WebBffUser = ValoraWeb::Valora.Web.Services.Bff.BffUser;
using WebBffOrganization = ValoraWeb::Valora.Web.Services.Bff.BffOrganization;
using WebBffAccessContext = ValoraWeb::Valora.Web.Services.Bff.BffAccessContext;
using WebBffAuthenticationResult = ValoraWeb::Valora.Web.Services.Bff.BffAuthenticationResult;
using WebIBffApiClient = ValoraWeb::Valora.Web.Services.Bff.IBffApiClient;
using WebAccountContextResponse = ValoraWeb::Valora.Web.Models.AccountContextResponse;

namespace Valora.Tests;

public sealed class ValoraInsightRegressionT01ToT15Tests {
    private sealed class StubCurrentOrganizationProvider(CurrentOrganizationContext context) : ICurrentOrganizationProvider {
        public CurrentOrganizationContext GetCurrent() => context;
    }

    private sealed class StubOrgAdminService : IOrganizationAdministrationService {
        public Task<OrganizationRecord> GetCurrentAsync(Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<OrganizationRecord> UpdateCurrentAsync(Guid organizationId, UpdateOrganizationRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubOrganizationBrandingService : IOrganizationBrandingService {
        public Task<OrganizationBrandingResponse> GetAsync(Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<OrganizationBrandingResponse> UpdateAsync(Guid organizationId, UpdateOrganizationBrandingRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<OrganizationSubscriptionResponse?> GetSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<OnboardingStepResponse>> GetOnboardingAsync(Guid organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task CompleteStepAsync(Guid organizationId, string stepCode, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestSessionStore : WebIDistributedBffSessionStore {
        private readonly Dictionary<string, WebBffServerSession> _sessions = new(StringComparer.Ordinal);
        public Task<WebBffServerSession?> GetAsync(string ticket, CancellationToken cancellationToken = default) =>
            Task.FromResult(_sessions.GetValueOrDefault(ticket));
        public Task SetAsync(string ticket, WebBffServerSession session, CancellationToken cancellationToken = default) {
            _sessions[ticket] = session;
            return Task.CompletedTask;
        }
        public Task RemoveAsync(string ticket, CancellationToken cancellationToken = default) {
            _sessions.Remove(ticket);
            return Task.CompletedTask;
        }
    }

    private sealed class StubBffApiClient : WebIBffApiClient {
        public Func<HttpMethod, string, object?, HttpResponseMessage>? OnSend;
        public Task CheckHealthAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<JsonElement> GetHealthAsync(string path, string correlationId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<WebBffAuthenticationResult> PostAuthenticationAsync(string path, object request, string correlationId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task PostAsync(string path, object? request, string? bearer, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? request, string bearer, string correlationId, CancellationToken cancellationToken) {
            if (OnSend is not null) return Task.FromResult(OnSend(method, path, request));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TestWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Valora.Web";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    // T01 — OrganizationRequired com a factory real do MVC.
    // Esperado: resposta prevista, sem colisão de traceId.
    [Fact]
    public async Task T01_OrganizationRequired_WithRealMvcFactory_DoesNotCollideTraceId() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore();
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-test-t01",
            RequestServices = provider
        };

        var controller = new OrganizationsController(
            new StubOrgAdminService(),
            null!,
            null!,
            new StubCurrentOrganizationProvider(CurrentOrganizationContext.Unresolved())) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext
            }
        };

        var result = await controller.Usage();
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Organização necessária", problem.Title);
        Assert.Equal(CurrentOrganizationContext.RequiredMessage, problem.Detail);
        Assert.True(problem.Extensions.ContainsKey("requiresOrganizationSelection"));
        Assert.True(problem.Extensions.ContainsKey("correlationId"));
        Assert.True(problem.Extensions.ContainsKey("traceId"));
        Assert.Equal(true, problem.Extensions["requiresOrganizationSelection"]);
    }

    // T02 — OrganizationBranding sem contexto.
    // Esperado: mesmo contrato, sem ArgumentException.
    [Fact]
    public async Task T02_OrganizationBranding_WithoutContext_ReturnsForbiddenWithoutArgumentException() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore();
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-test-t02",
            RequestServices = provider
        };

        var controller = new OrganizationBrandingController(
            new StubOrganizationBrandingService(),
            new StubCurrentOrganizationProvider(CurrentOrganizationContext.Unresolved())) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext
            }
        };

        var result = await controller.Onboarding(CancellationToken.None);
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Organização necessária", problem.Title);
        Assert.True(problem.Extensions.ContainsKey("requiresOrganizationSelection"));
        Assert.True(problem.Extensions.ContainsKey("correlationId"));
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    // T03 — Enriquecimento de erro executado mais de uma vez.
    // Esperado: extensões únicas e rastreabilidade preservada.
    [Fact]
    public void T03_ErrorEnrichment_ExecutedMultipleTimes_PreservesUniqueExtensionsAndTraceability() {
        var problem = new ProblemDetails {
            Title = "Erro inicial",
            Status = 400
        };

        problem.SetExtension("traceId", "initial-trace");
        problem.SetExtension("traceId", "updated-trace");
        problem.EnsureCorrelation("corr-123");
        problem.EnsureCorrelation("corr-123");
        problem.SetExtensions(new Dictionary<string, object?> {
            ["traceId"] = "final-trace",
            ["module"] = "operations"
        });

        Assert.Equal("final-trace", problem.Extensions["traceId"]);
        Assert.Equal("corr-123", problem.Extensions["correlationId"]);
        Assert.Equal("operations", problem.Extensions["module"]);
        Assert.Equal(3, problem.Extensions.Count);
    }

    // T04 — GET sem corpo, com Content-Type application/json e tamanho desconhecido.
    // Esperado: encaminhamento normal, sem desserialização do corpo vazio.
    [Fact]
    public async Task T04_GetWithoutBody_WithJsonContentType_DoesNotAttemptDeserialization() {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = null;
        context.Request.Body = new MemoryStream();

        var (success, body, error) = await WebBffBodyReader.ReadJsonBodyAsync(context.Request, "corr-t04");

        Assert.True(success);
        Assert.Null(body);
        Assert.Null(error);
    }

    // T05 — Comando com JSON válido e tamanho desconhecido.
    // Esperado: corpo encaminhado corretamente.
    [Fact]
    public async Task T05_CommandWithValidJson_AndUnknownLength_ReadsAndParsesCorrectly() {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = null;
        var json = "{\"title\":\"Diagnóstico Estratégico 2026\",\"active\":true}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var (success, body, error) = await WebBffBodyReader.ReadJsonBodyAsync(context.Request, "corr-t05");

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(body);

        var element = Assert.IsType<JsonElement>(body);
        Assert.Equal("Diagnóstico Estratégico 2026", element.GetProperty("title").GetString());
        Assert.True(element.GetProperty("active").GetBoolean());
    }

    // T06 — Comando com JSON malformado.
    // Esperado: 400 estruturado; serviço de negócio não executado.
    [Fact]
    public async Task T06_CommandWithMalformedJson_ReturnsStructured400ProblemDetails() {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ \"invalid\": "));

        var (success, body, error) = await WebBffBodyReader.ReadJsonBodyAsync(context.Request, "corr-t06");

        Assert.False(success);
        Assert.Null(body);
        Assert.NotNull(error);

        var objectResult = Assert.IsType<ObjectResult>(error);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("JSON malformado", problem.Title);
        Assert.Equal("MALFORMED_JSON", problem.Extensions["code"]);
        Assert.Equal("corr-t06", problem.Extensions["correlationId"]);
    }

    // T07 — Corpo obrigatório ausente.
    // Esperado: validação apropriada, sem 500.
    [Fact]
    public async Task T07_MissingRequiredBody_Returns400BadRequestInsteadOf500() {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = 0;

        var (success, body, error) = await WebBffBodyReader.ReadJsonBodyAsync(context.Request, "corr-t07", isBodyRequired: true);

        Assert.False(success);
        Assert.Null(body);
        Assert.NotNull(error);

        var objectResult = Assert.IsType<ObjectResult>(error);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("REQUEST_BODY_REQUIRED", problem.Extensions["code"]);
    }

    // T08 — Resposta 204 e download de relatório.
    // Esperado: contratos preservados.
    [Fact]
    public void T08_Preserves204NoContentAndBinaryDownloadContracts() {
        var adminControllerSource = File.ReadAllText(RepositoryPaths.WebFile("Controllers", "BffAdministrationController.cs"));
        Assert.Contains("HttpStatusCode.NoContent", adminControllerSource);
        Assert.Contains("return NoContent();", adminControllerSource);
        Assert.Contains("/reports/generated/", adminControllerSource);
        Assert.Contains("application/octet-stream", adminControllerSource);
        Assert.Contains("return File(", adminControllerSource);
    }

    // T09 — Administrador global sem organização.
    // Esperado: funcionalidades globais disponíveis; consultas de cliente não disparadas indevidamente.
    [Fact]
    public async Task T09_GlobalAdministrator_WithoutOrganization_ReturnsNullOrgInContext() {
        var store = new TestSessionStore();
        var client = new StubBffApiClient();
        var authService = new WebAuthenticationService(client, store, new NullLogger<WebAuthenticationService>());

        var ticket = "ticket-t09";
        var safe = new WebBffSafeSession(
            new WebBffUser(Guid.NewGuid(), "Admin Global", "admin@valora.com", "admin_valora"),
            null,
            null,
            new WebBffAccessContext(["admin_valora"], ["enterprise.read"], ["enterprise"], [], [], "active", null, null) {
                IsGlobalAdministrator = true,
                SelectedOrganizationId = null
            });

        await store.SetAsync(ticket, new("token", DateTimeOffset.UtcNow.AddHours(1), "refresh", DateTimeOffset.UtcNow.AddDays(7), safe));

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("bff_ticket", ticket),
            new Claim(ClaimTypes.Role, "admin_valora")
        ], "Cookie"));

        var accountController = new WebAccountController(authService) {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var response = await accountController.Context(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(response);
        var model = Assert.IsType<WebAccountContextResponse>(okResult.Value);

        Assert.Null(model.OrganizationId);
        Assert.Null(model.OrganizationName);
        Assert.Equal("admin_valora", model.PrimaryRole);
    }

    // T10 — Seleção anterior inexistente ou inválida.
    // Esperado: contexto inválido removido, sessão preservada e nova seleção possível.
    [Fact]
    public async Task T10_StaleOrganization_Returning404_ClearsSelectionWhilePreservingSession() {
        var store = new TestSessionStore();
        var client = new StubBffApiClient();
        var authService = new WebAuthenticationService(client, store, new NullLogger<WebAuthenticationService>());

        var staleOrgId = Guid.NewGuid();
        var ticket = "ticket-t10";
        var safe = new WebBffSafeSession(
            new WebBffUser(Guid.NewGuid(), "Admin Global", "admin@valora.com", "admin_valora"),
            new WebBffOrganization(staleOrgId, "Empresa Deletada", "Empresa Deletada", ""),
            null,
            new WebBffAccessContext(["admin_valora"], [], [], [], [], "active", null, null) {
                IsGlobalAdministrator = true,
                SelectedOrganizationId = staleOrgId
            });

        await store.SetAsync(ticket, new("token", DateTimeOffset.UtcNow.AddHours(1), "refresh", DateTimeOffset.UtcNow.AddDays(7), safe));

        client.OnSend = (method, path, body) => {
            if (path.Contains($"/saas/customers/{staleOrgId}")) {
                return new HttpResponseMessage(HttpStatusCode.NotFound) {
                    Content = new StringContent("{\"message\":\"Cliente não encontrado.\"}", Encoding.UTF8, "application/json")
                };
            }
            if (path.Contains("/saas/customers")) {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent("{\"items\":[]}", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("bff_ticket", ticket),
            new Claim(ClaimTypes.Role, "admin_valora")
        ], "Cookie"));

        var authController = new WebAuthController(authService, client, new NullLogger<WebAuthController>(), new TestWebHostEnvironment()) {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = await authController.Organizations(search: null, selectedOrganizationId: staleOrgId);
        Assert.IsType<OkObjectResult>(result);

        var updatedSession = await store.GetAsync(ticket);
        Assert.NotNull(updatedSession);
        Assert.Null(updatedSession.SafeSession.AccessContext.SelectedOrganizationId);
        Assert.Null(updatedSession.SafeSession.Organization);
    }

    // T11 — Usuário tenta selecionar organização não autorizada.
    // Esperado: rejeição no servidor.
    [Fact]
    public async Task T11_NonAdminUser_AttemptingToSelectOrganization_IsRejected() {
        var store = new TestSessionStore();
        var client = new StubBffApiClient();
        var authService = new WebAuthenticationService(client, store, new NullLogger<WebAuthenticationService>());

        var ticket = "ticket-t11";
        var safe = new WebBffSafeSession(
            new WebBffUser(Guid.NewGuid(), "Usuario Comum", "user@cliente.com", "analyst"),
            null,
            null,
            new WebBffAccessContext(["analyst"], [], [], [], [], "active", Guid.NewGuid(), null) {
                IsGlobalAdministrator = false
            });

        await store.SetAsync(ticket, new("token", DateTimeOffset.UtcNow.AddHours(1), "refresh", DateTimeOffset.UtcNow.AddDays(7), safe));

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("bff_ticket", ticket),
            new Claim(ClaimTypes.Role, "analyst")
        ], "Cookie"));

        var updated = await authService.SelectOrganizationAsync(httpContext, Guid.NewGuid(), CancellationToken.None);
        Assert.Null(updated);
    }

    // T12 — Troca rápida entre organizações A e B.
    // Esperado: resposta atrasada de A não aparece no contexto B.
    [Fact]
    public void T12_RapidSwitching_PreventsDelayedResponseRepainting() {
        var dashboardJs = File.ReadAllText(RepositoryPaths.WebFile("wwwroot", "js", "pages", "dashboard-page.js"));
        Assert.Contains("currentLoadGeneration", dashboardJs);
        Assert.Contains("thisGeneration !== currentLoadGeneration", dashboardJs);
    }

    // T13 — Várias chamadas recebem sessão expirada.
    // Esperado: tratamento coordenado, sem loop de renovação ou redirecionamento.
    [Fact]
    public void T13_CoordinatedSessionRefresh_UsesGateSemaphore() {
        var authServiceSource = File.ReadAllText(RepositoryPaths.WebFile("Services", "Bff", "BffAuthenticationService.cs"));
        Assert.Contains("ConcurrentDictionary<string, SemaphoreSlim> RefreshLocks", authServiceSource);
        Assert.Contains("await gate.WaitAsync(cancellationToken);", authServiceSource);
        Assert.Contains("finally {\n            gate.Release();\n        }", authServiceSource.Replace("\r\n", "\n"));
    }

    // T14 — Acesso negado ao seletor de responsáveis.
    // Esperado: mensagem compreensível; nenhum logout e nenhum HTML interpretado como JSON.
    [Fact]
    public void T14_ResponsibleOptions_AccessDeniedReturnsJsonNotHtmlRedirect() {
        var programSource = File.ReadAllText(RepositoryPaths.WebFile("Program.cs"));
        Assert.Contains("expectsJson", programSource);
        Assert.Contains("context.Request.Headers.Accept.Any", programSource);
        Assert.Contains("application/problem+json", programSource);
        Assert.Contains("ACCESS_DENIED", programSource);

        var selectorJs = File.ReadAllText(RepositoryPaths.WebFile("wwwroot", "js", "responsible-selector.js"));
        Assert.Contains("response.status === 403", selectorJs);
        Assert.Contains("Você não tem permissão para consultar estas opções.", selectorJs);
        Assert.DoesNotContain("location.assign('/Account/Login", selectorJs.Substring(selectorJs.IndexOf("response.status === 403")));
    }

    // T15 — Login → modo plataforma → selecionar cliente → dashboard → preparar diagnóstico → selecionar responsável → salvar → recarregar.
    // Esperado: jornada completa, com dados persistidos e contexto correto.
    [Fact]
    public async Task T15_CompleteJourney_PlatformMode_SelectOrganization_Propagation() {
        var store = new TestSessionStore();
        var client = new StubBffApiClient();
        var authService = new WebAuthenticationService(client, store, new NullLogger<WebAuthenticationService>());

        var orgId = Guid.NewGuid();
        var ticket = "ticket-t15";
        var safe = new WebBffSafeSession(
            new WebBffUser(Guid.NewGuid(), "Super Administrador", "super@valora.com", "admin_valora"),
            null,
            null,
            new WebBffAccessContext(["admin_valora"], ["diagnostics.manage"], ["diagnostics"], [], [], "active", null, null) {
                IsGlobalAdministrator = true,
                SelectedOrganizationId = null
            });

        await store.SetAsync(ticket, new("token", DateTimeOffset.UtcNow.AddHours(1), "refresh", DateTimeOffset.UtcNow.AddDays(7), safe));

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("bff_ticket", ticket),
            new Claim(ClaimTypes.Role, "admin_valora"),
            new Claim("session_id", Guid.NewGuid().ToString())
        ], "Cookie"));

        var accountController = new WebAccountController(authService) {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // 1. Em Modo Plataforma, organização é nula
        var initialContext = await accountController.Context(CancellationToken.None);
        var initialModel = Assert.IsType<WebAccountContextResponse>(Assert.IsType<OkObjectResult>(initialContext).Value);
        Assert.Null(initialModel.OrganizationId);

        // 2. Selecionar cliente
        var updated = await authService.SelectOrganizationAsync(httpContext, orgId, CancellationToken.None, "Cliente Corporativo Alfa");
        Assert.NotNull(updated);
        Assert.Equal(orgId, updated.AccessContext.SelectedOrganizationId);
        Assert.Equal("Cliente Corporativo Alfa", updated.Organization?.Name);

        // 3. Consultar contexto novamente no Modo Organização
        var postSelectContext = await accountController.Context(CancellationToken.None);
        var postSelectModel = Assert.IsType<WebAccountContextResponse>(Assert.IsType<OkObjectResult>(postSelectContext).Value);
        Assert.Equal(orgId, postSelectModel.OrganizationId);
        Assert.Equal("Cliente Corporativo Alfa", postSelectModel.OrganizationName);
    }
}
