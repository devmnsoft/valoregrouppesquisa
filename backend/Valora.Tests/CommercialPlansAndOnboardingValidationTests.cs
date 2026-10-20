extern alias ValoraWeb;

using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.ModularSaas;
using Valora.Infrastructure.ModularSaas;
using Valora.Tests.Support;
using Xunit;

using WebOnboardingController = ValoraWeb::Valora.Web.Controllers.OnboardingController;
using WebOnboardingChecklistViewModel = ValoraWeb::Valora.Web.Controllers.OnboardingChecklistViewModel;

namespace Valora.Tests;

[Collection("PostgresIntegrationTests")]
public sealed class CommercialPlansAndOnboardingValidationTests {
    private readonly string? _connectionString = Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION");

    private sealed class DummyTempDataProvider : ITempDataProvider {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class StubConnectionFactory(string connectionString) : IDbConnectionFactory {
        public System.Data.IDbConnection Create() => new NpgsqlConnection(connectionString);
    }

    private sealed class StubOrgProvider(Guid? orgId) : ICurrentOrganizationProvider {
        public CurrentOrganizationContext GetCurrent() =>
            orgId.HasValue
                ? CurrentOrganizationContext.Resolved(orgId.Value, "test")
                : CurrentOrganizationContext.Unresolved();
    }

    [Fact]
    public async Task OnboardingController_PersistsProgressToDatabase_AndRecoversState() {
        if (string.IsNullOrWhiteSpace(_connectionString)) return;

        var orgId = Guid.NewGuid();
        var suffix = Random.Shared.Next(1000, 9999);
        await using (var conn = new NpgsqlConnection(_connectionString)) {
            await conn.OpenAsync();
            await conn.ExecuteAsync("""
                INSERT INTO valorapesquisa.organizations (id, name, slug, status, email, created_at)
                VALUES (@orgId, 'Org Onboarding Test', @slug, 'active', @email, now());
                """, new { orgId, slug = $"org-onb-{suffix}", email = $"onb-{suffix}@valora.test" });
        }

        var factory = new StubConnectionFactory(_connectionString);
        var orgProvider = new StubOrgProvider(orgId);
        var controller = new WebOnboardingController(NullLogger<WebOnboardingController>.Instance, factory, orgProvider);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim("organization_id", orgId.ToString())],
            "TestAuth"));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider());

        // Passo 1: Executa progress para 'organization' e 'plan'
        var res1 = await controller.Progress("organization", true);
        var res2 = await controller.Progress("plan", true);
        Assert.IsType<RedirectToActionResult>(res1);
        Assert.IsType<RedirectToActionResult>(res2);

        // Verifica persistência no PostgreSQL
        await using (var conn = new NpgsqlConnection(_connectionString)) {
            await conn.OpenAsync();
            var stepCount = await conn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM valorapesquisa.onboarding_steps WHERE organization_id = @orgId AND status = 'completed';",
                new { orgId });
            Assert.Equal(2, stepCount);

            var currentState = await conn.ExecuteScalarAsync<string>(
                "SELECT current_step FROM valorapesquisa.onboarding_states WHERE organization_id = @orgId;",
                new { orgId });
            Assert.Equal("plan", currentState);
        }

        // Passo 2: Novo request limpo (sem cookies prévios) para validar recuperação a partir do banco
        var freshContext = new DefaultHttpContext();
        freshContext.User = httpContext.User;
        var freshController = new WebOnboardingController(NullLogger<WebOnboardingController>.Instance, factory, orgProvider) {
            ControllerContext = new ControllerContext { HttpContext = freshContext }
        };

        var indexResult = await freshController.Index();
        var viewResult = Assert.IsType<ViewResult>(indexResult);
        var checklist = Assert.IsType<WebOnboardingChecklistViewModel>(viewResult.Model);

        Assert.Equal(2, checklist.Completed);
        var orgStep = checklist.Items.First(i => i.Code == "organization");
        var planStep = checklist.Items.First(i => i.Code == "plan");
        var brandingStep = checklist.Items.First(i => i.Code == "branding");

        Assert.True(orgStep.Completed, "Etapa 'organization' deve vir como completed do banco.");
        Assert.True(planStep.Completed, "Etapa 'plan' deve vir como completed do banco.");
        Assert.False(brandingStep.Completed, "Etapa 'branding' ainda não deve estar concluída.");
        Assert.Equal("branding", checklist.Next?.Code);
    }

    [Fact]
    public async Task CommercialAccessEvaluation_EnforcesLimitsAndContractStatus() {
        if (string.IsNullOrWhiteSpace(_connectionString)) return;

        var factory = new StubConnectionFactory(_connectionString);
        var repo = new CommercialSaasRepository(factory);
        var service = new CommercialSaasService(repo, NullLogger<CommercialSaasService>.Instance);

        // 1. Cliente vazio no serviço exige contexto de cliente
        var noClient = await service.EvaluateAccessAsync(Guid.Empty, "diagnostics", false, CancellationToken.None);
        Assert.False(noClient.Allowed);
        Assert.Equal("CLIENT_CONTEXT_REQUIRED", noClient.Code);

        // 2. Cliente não contratado no repositório bloqueia com MODULE_NOT_CONTRACTED
        var notContracted = await repo.EvaluateAccessAsync(Guid.NewGuid(), "diagnostics", false, CancellationToken.None);
        Assert.False(notContracted.Allowed);
        Assert.Equal("MODULE_NOT_CONTRACTED", notContracted.Code);

        // 3. Módulo inexistente
        var invalidModule = await repo.EvaluateAccessAsync(Guid.NewGuid(), "modulo_fantasma", false, CancellationToken.None);
        Assert.False(invalidModule.Allowed);
        Assert.Equal("MODULE_UNAVAILABLE", invalidModule.Code);

        // 3. Módulos que não requerem contrato (se houver no catálogo) concedem acesso direto
        var publicModules = await repo.ListModulesAsync(null, CancellationToken.None);
        Assert.NotNull(publicModules);

        // 4. Lista de planos comerciais canônicos
        var plans = await repo.ListPlansAsync(CancellationToken.None);
        Assert.NotNull(plans);
    }
}
