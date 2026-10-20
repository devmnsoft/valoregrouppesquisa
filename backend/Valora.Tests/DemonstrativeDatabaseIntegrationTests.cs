using System;
using System.IO;
using Valora.Domain.Methodology;
using Xunit;

namespace Valora.Tests;

public class DemonstrativeDatabaseIntegrationTests {
    private static string GetScriptContent(string scriptFilename) {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "database", "postgresql", scriptFilename);
        if (!File.Exists(path)) {
            path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "database", "postgresql", scriptFilename));
        }
        if (!File.Exists(path)) {
            path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "database", "postgresql", scriptFilename));
        }

        Assert.True(File.Exists(path), $"{scriptFilename} não encontrado. Caminho procurado: {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void SeedScript_MustContainAllThreeDemonstrativeCompaniesWithFullScenarios() {
        var script = GetScriptContent("seed_homologacao.sql");

        var techOrg = "11111111-1111-1111-1111-111111111111";
        var indOrg = "22222222-2222-2222-2222-222222222222";
        var srvOrg = "33333333-3333-3333-3333-333333333333";

        // 1. Organizações de exemplo presentes
        Assert.Contains("Empresa Exemplo — Tecnologia", script);
        Assert.Contains("Empresa Exemplo — Indústria", script);
        Assert.Contains("Empresa Exemplo — Serviços", script);
        Assert.Contains(techOrg, script);
        Assert.Contains(indOrg, script);
        Assert.Contains(srvOrg, script);

        // 2. Unidades e Departamentos estruturados
        Assert.Contains("Sede Tecnologia Belém", script);
        Assert.Contains("Planta Matriz Indústria", script);
        Assert.Contains("Unidade Central de Serviços", script);

        // 3. Questionários e Perguntas para as 3 empresas
        Assert.Contains("form_maturidade_demo", script);
        Assert.Contains("form_eficiencia_industria", script);
        Assert.Contains("form_governanca_servicos", script);
        Assert.Contains("lid_comunicacao", script);
        Assert.Contains("ind_recursos", script);
        Assert.Contains("srv_qualidade", script);

        // 4. Pesquisas e Ciclos Diagnósticos ativos para as 3 empresas
        Assert.Contains("fe111111-1111-1111-1111-111111111111", script);
        Assert.Contains("fe222222-2222-2222-2222-222222222222", script);
        Assert.Contains("fe333333-3333-3333-3333-333333333333", script);
        Assert.Contains("c1111111-1111-1111-1111-111111111111", script);
        Assert.Contains("c2222222-2222-2222-2222-222222222222", script);
        Assert.Contains("c3333333-3333-3333-3333-333333333333", script);

        // 5. Links canônicos e Campanhas
        Assert.Contains("51000000-1111-1111-1111-111111111111", script);
        Assert.Contains("51000000-2222-2222-2222-222222222222", script);
        Assert.Contains("51000000-3333-3333-3333-333333333333", script);
        Assert.Contains("ca111111-1111-1111-1111-111111111111", script);
        Assert.Contains("ca222222-2222-2222-2222-222222222222", script);
        Assert.Contains("ca333333-3333-3333-3333-333333333333", script);

        // 6. Evidências metodológicas para as 3 empresas
        Assert.Contains("e1111111-1111-1111-1111-111111111111", script);
        Assert.Contains("e2222222-2222-2222-2222-222222222221", script);
        Assert.Contains("e3333333-3333-3333-3333-333333333331", script);

        // 7. Achados AI e Planos de Ação
        Assert.Contains("ba111111-1111-1111-1111-111111111111", script);
        Assert.Contains("ba222222-2222-2222-2222-222222222221", script);
        Assert.Contains("ba333333-3333-3333-3333-333333333331", script);
        Assert.Contains("aa111111-1111-1111-1111-111111111111", script);
        Assert.Contains("aa222222-2222-2222-2222-222222222222", script);
        Assert.Contains("aa333333-3333-3333-3333-333333333333", script);

        // 8. Certificados metodológicos
        Assert.Contains("valorapesquisa.certificates", script);
        Assert.Contains("VAL-", script);
    }

    [Fact]
    public void SeedScript_ResultScores_MustBeMathematicallyCalculatedFromAnswers() {
        var script = GetScriptContent("seed_homologacao.sql");

        // 1. O script NÃO deve ter inserção arbitrária estática de scores fabricados
        Assert.DoesNotContain("VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', v_resp_id, 105, 125, 84.0, 'Consolidado'", script);

        // 2. O script deve calcular os scores diretamente via agregação matemática das respostas reais
        Assert.Contains("SUM(ra.score)::int AS total_score", script);
        Assert.Contains("SUM(ra.max_score)::int AS max_score", script);
        Assert.Contains("ROUND((SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0, 1) AS percentage", script);

        // 3. O script deve mapear os níveis oficiais da Metodologia Valora
        Assert.Contains("WHEN (SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0 <= 25.0 THEN 'Inicial'", script);
        Assert.Contains("WHEN (SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0 <= 50.0 THEN 'Estruturante'", script);
        Assert.Contains("WHEN (SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0 <= 75.0 THEN 'Integrado'", script);
        Assert.Contains("ELSE 'Maduro'", script);
    }

    [Theory]
    [InlineData(20.0, "Inicial", "initial")]
    [InlineData(25.0, "Inicial", "initial")]
    [InlineData(35.0, "Estruturante", "structuring")]
    [InlineData(50.0, "Estruturante", "structuring")]
    [InlineData(64.0, "Integrado", "integrated")]
    [InlineData(75.0, "Integrado", "integrated")]
    [InlineData(90.0, "Maduro", "mature")]
    [InlineData(100.0, "Maduro", "mature")]
    public void OfficialMethodology_OrganizationalMaturity_ClassificationContract(
        decimal score,
        string expectedLabel,
        string expectedCode) {
        Assert.Equal(expectedLabel, OrganizationalMaturity.Label(score));
        Assert.Equal(expectedCode, OrganizationalMaturity.Code(score));
    }

    [Fact]
    public void ResetHomologacaoScript_MustHaveHardenedGuardrailAndFullCleanup() {
        var script = GetScriptContent("reset_homologacao.sql");

        // 1. Guardrail rigoroso contra produção
        Assert.Contains("current_database() ~* '(prod|production|live|prd)'", script);
        Assert.Contains("valora.allow_demo_seed=true", script);

        // 2. Limpeza completa das 3 organizações de demonstração
        Assert.Contains("DELETE FROM valorapesquisa.certificates WHERE organization_id IN", script);
        Assert.Contains("DELETE FROM valorapesquisa.result_scores WHERE organization_id IN", script);
        Assert.Contains("DELETE FROM valorapesquisa.responses WHERE organization_id IN", script);
        Assert.Contains("DELETE FROM valorapesquisa.surveys WHERE organization_id IN", script);
        Assert.Contains("DELETE FROM valorapesquisa.action_plans WHERE organization_id IN", script);
        Assert.Contains("DELETE FROM valorapesquisa.action_items WHERE organization_id IN", script);
        Assert.Contains("DELETE FROM valorapesquisa.valora_ai_insights WHERE organization_id IN", script);
    }
}
