extern alias ValoraWeb;

using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Contracts;
using Valora.Application.ModularSaas;
using Valora.Tests.Support;
using WebProgram = ValoraWeb::Program;

namespace Valora.Tests;

public sealed class EvolutionHttpAccessTests {
    [Theory]
    [InlineData("", false)]
    [InlineData("evolution.manage", false)]
    [InlineData("evolution.read", true)]
    [InlineData("evolution.read,evolution.manage", true)]
    public async Task Http_read_requires_explicit_read_permission(string permissions, bool allowed) {
        using var factory = Factory(permissions, ModuleAccessDecision.Granted());
        using var client = Client(factory);
        var response = await client.GetAsync("/Evolution");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(allowed ? "/Evolution/Cycles" : "/error/403", response.Headers.Location!.IsAbsoluteUri ? response.Headers.Location.PathAndQuery : response.Headers.Location.OriginalString);
    }

    [Theory]
    [InlineData("evolution.read", "Open")]
    [InlineData("evolution.read,evolution.snapshots.generate", "Open")]
    [InlineData("evolution.read,evolution.manage", "Snapshot")]
    public async Task Http_command_requires_its_own_permission(string permissions, string command) {
        using var factory = Factory(permissions, ModuleAccessDecision.Granted());
        using var client = Client(factory);
        var route = command == "Open" ? "/Evolution/Cycles/Open" : $"/Evolution/Cycles/{Guid.NewGuid()}/Snapshot";
        var response = await client.PostAsync(route, new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/error/403", response.Headers.Location!.IsAbsoluteUri ? response.Headers.Location.PathAndQuery : response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task Authorized_command_still_requires_antiforgery() {
        using var factory = Factory("evolution.read,evolution.manage", ModuleAccessDecision.Granted());
        using var client = Client(factory);
        var response = await client.PostAsync("/Evolution/Cycles/Open", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Http_commercial_restriction_denies_write_before_command(bool readOnly) {
        var decision = readOnly ? ModuleAccessDecision.Granted(true) : ModuleAccessDecision.Denied("MODULE_NOT_CONTRACTED", "Recurso não contratado.");
        using var factory = Factory("evolution.read,evolution.manage", decision);
        using var client = Client(factory);
        var response = await client.PostAsync("/Evolution/Cycles/Open", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(decision.Code, await response.Content.ReadAsStringAsync());
    }

    [PostgresFact("renderização HTTP dos ciclos não executada.")]
    public async Task Read_only_user_sees_cycles_without_creation_command() {
        using var factory = Factory("evolution.read", ModuleAccessDecision.Granted());
        using var client = Client(factory);
        var response = await client.GetAsync("/Evolution/Cycles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("evolution-cycles", html);
        Assert.DoesNotContain("data-cycle-dialog", html);
        Assert.DoesNotContain("data-action=\"new-cycle\"", html);
    }

    [PostgresFact("renderização HTTP com módulo somente leitura não executada.")]
    public async Task Read_only_module_hides_creation_even_for_manager() {
        using var factory = Factory("evolution.read,evolution.manage", ModuleAccessDecision.Granted(true));
        using var client = Client(factory);
        var response = await client.GetAsync("/Evolution/Cycles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("data-cycle-dialog", html);
        Assert.Contains("somente para consulta", html);
    }

    [PostgresFact("jornada HTTP de ciclo manual não executada.")]
    public async Task Manual_cycle_can_be_created_monitored_reopened_and_closed_through_http() {
        var organization = Guid.NewGuid();
        await using var db = new NpgsqlConnection(PostgresTestEnvironment.ConnectionString("jornada HTTP"));
        await db.ExecuteAsync("INSERT INTO valorapesquisa.organizations(id,name,slug) VALUES(@organization,'HTTP synthetic test',@slug)", new { organization, slug = organization.ToString("N") });
        try {
            using var factory = Factory("evolution.read,evolution.manage,evolution.snapshots.generate", ModuleAccessDecision.Granted(), organization);
            using var client = Client(factory);
            var page = await client.GetStringAsync("/Evolution/Cycles");
            Assert.Contains("data-cycle-dialog", page);
            var token = WebUtility.HtmlDecode(Regex.Match(page, "name=\"csrf-token\" content=\"([^\"]+)\"").Groups[1].Value);
            Assert.NotEmpty(token);
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
            var fields = new Dictionary<string, string> {
                ["Title"] = "Ciclo HTTP sintético",
                ["Summary"] = "Objetivo preservado",
                ["ReferenceMode"] = "manual",
                ["BaselineScore"] = "0",
                ["PeriodStart"] = "2026-10-01",
                ["EvidenceSummary"] = "Referência manual declarada"
            };
            var created = await client.PostAsync("/Evolution/Cycles/Open", new FormUrlEncodedContent(fields));
            Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
            var details = created.Headers.Location!.OriginalString;
            var id = await db.QuerySingleAsync<Guid>("SELECT id FROM valorapesquisa.evolution_cycles WHERE organization_id=@organization", new { organization });
            var reopened = await client.GetStringAsync(details);
            Assert.Contains("Objetivo preservado", reopened);
            var monitoring = await client.PostAsync($"/Evolution/Cycles/{id}/Snapshot", new FormUrlEncodedContent(new Dictionary<string, string> {
                ["Evidence"] = "Evidência de acompanhamento",
                ["Interpretation"] = "Sem comprovação de melhoria",
                ["Recommendation"] = "Reavaliar com nova coleta"
            }));
            Assert.Equal(HttpStatusCode.Redirect, monitoring.StatusCode);
            Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.evolution_snapshots WHERE organization_id=@organization", new { organization }));
            var version = await db.QuerySingleAsync<long>("SELECT version FROM valorapesquisa.evolution_cycles WHERE id=@id AND organization_id=@organization", new { id, organization });
            var closed = await client.PostAsync($"/Evolution/Cycles/{id}/Close", new FormUrlEncodedContent(new Dictionary<string, string> {
                ["version"] = version.ToString(),
                ["learning"] = "Síntese preservada",
                ["pendingTreatment"] = "Solicitar medição posterior"
            }));
            Assert.Equal(HttpStatusCode.Redirect, closed.StatusCode);
            Assert.Equal("closed", await db.QuerySingleAsync<string>("SELECT status FROM valorapesquisa.evolution_cycles WHERE id=@id AND organization_id=@organization", new { id, organization }));
            Assert.Contains("Solicitar medição posterior", WebUtility.HtmlDecode(await client.GetStringAsync(details)));
            Assert.Equal(0, await db.QuerySingleAsync<decimal>("SELECT baseline_score FROM valorapesquisa.evolution_cycles WHERE id=@id AND organization_id=@organization", new { id, organization }));
        }
        finally {
            await db.ExecuteAsync("DELETE FROM valorapesquisa.journey_events WHERE organization_id=@organization; DELETE FROM valorapesquisa.evolution_snapshots WHERE organization_id=@organization; DELETE FROM valorapesquisa.evolution_cycles WHERE organization_id=@organization; DELETE FROM valorapesquisa.organizations WHERE id=@organization", new { organization });
        }
    }

    private static HttpClient Client(WebApplicationFactory<WebProgram> factory) => factory.CreateClient(new() {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    private static WebApplicationFactory<WebProgram> Factory(string permissions, ModuleAccessDecision decision, Guid? organization = null) =>
        new WebApplicationFactory<WebProgram>().WithWebHostBuilder(builder => {
            builder.UseEnvironment("Development");
            var connection = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");
            if (!string.IsNullOrWhiteSpace(connection)) {
                builder.UseSetting("ConnectionStrings:Postgres", connection);
                builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
            }
            builder.ConfigureServices(services => {
                services.AddSingleton(new TestIdentity(permissions, organization ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));
                services.AddAuthentication(options => {
                    options.DefaultAuthenticateScheme = "EvolutionTest";
                    options.DefaultChallengeScheme = "Cookies";
                    options.DefaultForbidScheme = "Cookies";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthentication>("EvolutionTest", _ => { });
                services.RemoveAll<IPermissionService>();
                services.AddScoped<IPermissionService, TestPermissions>();
                services.RemoveAll<ICommercialSaasRepository>();
                services.AddSingleton<ICommercialSaasRepository>(new TestCommercial(decision));
            });
        });

    public sealed record TestIdentity(string Permissions, Guid Organization);
    public sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, TestIdentity identity) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                new Claim("organization_id", identity.Organization.ToString()), new Claim("permission", identity.Permissions) };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
    private sealed class TestPermissions(TestIdentity identity) : IPermissionService {
        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? organizationId = null) =>
            Task.FromResult(identity.Permissions.Split(',').Contains(permissionCode));
    }
    private sealed class TestCommercial(ModuleAccessDecision decision) : ICommercialSaasRepository {
        public Task<ModuleAccessDecision> EvaluateAccessAsync(Guid clientId, string moduleCode, bool writeOperation, CancellationToken cancellationToken) => Task.FromResult(decision);
        public Task<IReadOnlyList<CommercialModule>> ListModulesAsync(Guid? clientId, CancellationToken c) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommercialPlan>> ListPlansAsync(CancellationToken c) => throw new NotSupportedException();
        public Task<bool> SetModuleStatusAsync(Guid clientId, string moduleCode, string status, Guid actorUserId, string reason, string correlationId, CancellationToken c) => throw new NotSupportedException();
        public Task RequestUpgradeAsync(Guid clientId, Guid actorUserId, string moduleCode, string reason, string correlationId, CancellationToken c) => throw new NotSupportedException();
    }
}
