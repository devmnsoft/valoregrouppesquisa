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
        public Func<string, object, WebBffAuthenticationResult>? OnPostAuth;
        public Task CheckHealthAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<JsonElement> GetHealthAsync(string path, string correlationId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<WebBffAuthenticationResult> PostAuthenticationAsync(string path, object request, string correlationId, CancellationToken cancellationToken) {
            if (OnPostAuth is not null) return Task.FromResult(OnPostAuth(path, request));
            throw new NotImplementedException();
        }
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

    private static HttpContext CreateTestHttpContext(string? ticket = null, string? role = null) {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(options => {
            options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie(options => {
            options.Cookie.Name = "valora.bff";
        });
        var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext {
            RequestServices = provider
        };
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(ticket)) claims.Add(new Claim("bff_ticket", ticket));
        if (!string.IsNullOrEmpty(role)) claims.Add(new Claim(ClaimTypes.Role, role));
        claims.Add(new Claim("session_id", Guid.NewGuid().ToString()));
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme));
        return httpContext;
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
    public async Task T08_Preserves204NoContentAndBinaryDownloadContracts() {
        var adminControllerSource = File.ReadAllText(RepositoryPaths.WebFile("Controllers", "BffAdministrationController.cs"));
        Assert.Contains("HttpStatusCode.NoContent", adminControllerSource);
        Assert.Contains("return NoContent();", adminControllerSource);

        var store = new TestSessionStore();
        var client = new StubBffApiClient();
        var authService = new WebAuthenticationService(client, store, new NullLogger<WebAuthenticationService>());

        var ticket = "ticket-t08";
        var safe = new WebBffSafeSession(
            new WebBffUser(Guid.NewGuid(), "Admin", "admin@valora.com", "admin_valora"),
            null, null,
            new WebBffAccessContext(["admin_valora"], ["reports.read"], ["reports"], [], [], "active", null, null));
        await store.SetAsync(ticket, new("token", DateTimeOffset.UtcNow.AddHours(1), "refresh", DateTimeOffset.UtcNow.AddDays(7), safe));

        // 1. Prova comportamental de 204 NoContent
        var httpContext = CreateTestHttpContext(ticket, "admin_valora");
        httpContext.Request.Method = "DELETE";
        client.OnSend = (m, p, b) => new HttpResponseMessage(HttpStatusCode.NoContent);

        var controller = new ValoraWeb::Valora.Web.Controllers.BffAdministrationController(client, authService, new NullLogger<ValoraWeb::Valora.Web.Controllers.BffAdministrationController>()) {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result204 = await controller.Reports("test-item", CancellationToken.None);
        Assert.IsType<NoContentResult>(result204);

        // 2. Prova comportamental de download binário
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
        httpContext.Request.Method = "GET";
        client.OnSend = (m, p, b) => {
            var msg = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new ByteArrayContent(pdfBytes)
            };
            msg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            msg.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") {
                FileName = "relatorio_executivo.pdf"
            };
            return msg;
        };

        var resultDownload = await controller.Reports("generated/123/download", CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(resultDownload);
        Assert.Equal(pdfBytes, fileResult.FileContents);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("relatorio_executivo.pdf", fileResult.FileDownloadName);
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

        var httpContext = CreateTestHttpContext(ticket, "admin_valora");

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
    public async Task T13_CoordinatedSessionRefresh_UsesGateSemaphore_AndAvoidsDuplicateRefresh() {
        var authServiceSource = File.ReadAllText(RepositoryPaths.WebFile("Services", "Bff", "BffAuthenticationService.cs"));
        Assert.Contains("ConcurrentDictionary<string, SemaphoreSlim> RefreshLocks", authServiceSource);
        Assert.Contains("await gate.WaitAsync(cancellationToken);", authServiceSource);

        var store = new TestSessionStore();
        var client = new StubBffApiClient();
        var authService = new WebAuthenticationService(client, store, new NullLogger<WebAuthenticationService>());

        var ticket = "ticket-t13";
        var safe = new WebBffSafeSession(
            new WebBffUser(Guid.NewGuid(), "Admin", "admin@valora.com", "admin_valora"),
            null, null,
            new WebBffAccessContext(["admin_valora"], [], [], [], [], "active", null, null));

        // Token expirado
        await store.SetAsync(ticket, new("expired_token", DateTimeOffset.UtcNow.AddMinutes(-5), "valid_refresh", DateTimeOffset.UtcNow.AddDays(7), safe));

        int refreshApiCallCount = 0;
        client.OnPostAuth = (path, req) => {
            Interlocked.Increment(ref refreshApiCallCount);
            // Simula latência de rede na API para forçar sobreposição real das requisições concorrentes
            Thread.Sleep(50);
            return new WebBffAuthenticationResult(
                "new_access_token_123", DateTimeOffset.UtcNow.AddHours(1),
                "new_refresh_token_456", DateTimeOffset.UtcNow.AddDays(7),
                Guid.NewGuid(),
                safe.User, null, null, safe.AccessContext);
        };

        var httpContext1 = CreateTestHttpContext(ticket, "admin_valora");
        var httpContext2 = CreateTestHttpContext(ticket, "admin_valora");

        // Executa duas chamadas concorrentes para o mesmo ticket
        // GetAsync detecta o token expirado e chama RefreshCoreAsync(context, false) sob controle do SemaphoreSlim
        var task1 = Task.Run(() => authService.GetAsync(httpContext1, CancellationToken.None));
        var task2 = Task.Run(() => authService.GetAsync(httpContext2, CancellationToken.None));

        await Task.WhenAll(task1, task2);

        // Comprova que apenas UMA chamada de renovação alcançou o backend de autenticação
        Assert.Equal(1, refreshApiCallCount);

        var sessionAfter = await store.GetAsync(ticket);
        Assert.NotNull(sessionAfter);
        Assert.Equal("new_access_token_123", sessionAfter.AccessToken);
    }

    // T14 — Acesso negado ao seletor de responsáveis.
    // Esperado: mensagem compreensível; nenhum logout e nenhum HTML interpretado como JSON.
    [Fact]
    public async Task T14_ResponsibleOptions_AccessDeniedReturnsJsonNotHtmlRedirect() {
        var selectorJs = File.ReadAllText(RepositoryPaths.WebFile("wwwroot", "js", "responsible-selector.js"));
        Assert.Contains("response.status === 403", selectorJs);
        Assert.Contains("Você não tem permissão para consultar estas opções.", selectorJs);
        Assert.DoesNotContain("location.assign('/Account/Login", selectorJs.Substring(selectorJs.IndexOf("response.status === 403")));

        // Prova comportamental real de que a infraestrutura HTTP do BFF retorna 403 JSON estruturado
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        httpContext.Request.Headers.Accept = "application/json";

        var redirectContext = new Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
            httpContext,
            new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                null,
                typeof(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationHandler)),
            new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions(),
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties(),
            "/Account/AccessDenied");

        await ValoraWeb::BffAuthenticationFailureHandler.WriteFailureAsync(
            redirectContext,
            StatusCodes.Status403Forbidden,
            "ACCESS_DENIED",
            "Você não tem permissão para executar esta ação.");

        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("ACCESS_DENIED", body);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    // T15 — Login → modo plataforma → selecionar cliente → dashboard → preparar diagnóstico → selecionar responsável → salvar → recarregar.
    // NOTA DE ESCOPO: Este teste valida a orquestração do pipeline BFF (WebAuthenticationService + BffDiagnosticWorkspaceController)
    // e os contratos de DTO com cliente simulado (StubBffApiClient). Ele NÃO substitui a prova de persistência real,
    // que é comprovada de ponta a ponta no banco PostgreSQL pelos testes em ValoraInsightFullJourneyIntegrationTests.
    [Fact]
    public async Task T15_CompleteJourney_PlatformMode_SelectOrganization_Propagation() {
        var store = new TestSessionStore();
        var client = new StubBffApiClient();
        var authService = new WebAuthenticationService(client, store, new NullLogger<WebAuthenticationService>());

        var orgId = Guid.NewGuid();
        var responsibleId = Guid.NewGuid();
        var diagId = Guid.NewGuid();
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

        var httpContext = CreateTestHttpContext(ticket, "admin_valora");

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

        // 4. Preparar diagnóstico e salvar responsável
        string persistedTitle = "Diagnóstico Estratégico 2026";
        string persistedAudience = "Lideranças Regionais";
        var persistedState = new {
            id = diagId,
            organizationId = orgId,
            title = persistedTitle,
            responsibleUserId = responsibleId,
            targetAudience = persistedAudience,
            rowVersion = 2
        };

        client.OnSend = (method, path, body) => {
            if (path.Contains($"/api/v1/diagnostics/{diagId}/workspace/preparation")) {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(JsonSerializer.Serialize(persistedState), Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        };

        var workspaceController = new ValoraWeb::Valora.Web.Controllers.BffDiagnosticWorkspaceController(client, authService) {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var prepPayload = JsonSerializer.Serialize(new {
            title = persistedTitle,
            responsibleUserId = responsibleId,
            targetAudience = persistedAudience,
            rowVersion = 1
        });
        httpContext.Request.Method = "PUT";
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(prepPayload));

        var saveResult = await workspaceController.Forward(diagId, "preparation", CancellationToken.None);
        var contentResult = Assert.IsType<ContentResult>(saveResult);
        Assert.Equal(200, contentResult.StatusCode);
        using (var doc = JsonDocument.Parse(contentResult.Content!)) {
            Assert.Equal(persistedTitle, doc.RootElement.GetProperty("title").GetString());
            Assert.Equal(responsibleId, doc.RootElement.GetProperty("responsibleUserId").GetGuid());
        }

        // 5. Reabrir e recarregar dados confirmando persistência
        httpContext.Request.Method = "GET";
        httpContext.Request.Body = new MemoryStream();
        var reloadResult = await workspaceController.Forward(diagId, "preparation", CancellationToken.None);
        var reloadContent = Assert.IsType<ContentResult>(reloadResult);
        Assert.Equal(200, reloadContent.StatusCode);
        using (var doc = JsonDocument.Parse(reloadContent.Content!)) {
            Assert.Equal(persistedAudience, doc.RootElement.GetProperty("targetAudience").GetString());
            Assert.Equal(responsibleId, doc.RootElement.GetProperty("responsibleUserId").GetGuid());
        }
    }
}
