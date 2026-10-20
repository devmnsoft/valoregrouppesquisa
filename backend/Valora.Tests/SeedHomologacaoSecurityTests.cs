using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Valora.Tests;

public class SeedHomologacaoSecurityTests {
    private static string GetSeedScriptContent() {
        var seedPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "database", "postgresql", "seed_homologacao.sql");
        if (!File.Exists(seedPath)) {
            seedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "database", "postgresql", "seed_homologacao.sql"));
        }
        if (!File.Exists(seedPath)) {
            seedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "database", "postgresql", "seed_homologacao.sql"));
        }

        Assert.True(File.Exists(seedPath), $"seed_homologacao.sql não encontrado. Caminho procurado: {seedPath}");
        return File.ReadAllText(seedPath);
    }

    [Fact]
    public void SeedScript_MustNotContainHardcodedPasswordsOrPredictableHashes() {
        var script = GetSeedScriptContent();

        // 1. Proibir o hash estático histórico
        Assert.DoesNotContain("$2a$11$s6GuFVkm7kQG/5Ko4VqUvO2V1KmWB51Ceqt/ctalZd.xrf/eqgQQ2", script);

        // 2. Proibir comentários com senhas compartilhadas previsíveis
        Assert.DoesNotContain("Senha de todos os usuários de teste: Admin@123", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SENHA: Admin@123", script, StringComparison.OrdinalIgnoreCase);

        // 3. Garantir uso de provisionamento dinâmico via pgcrypto ou configuração de sessão
        Assert.Contains("valora.demo_password_hash", script);
        Assert.Contains("crypt(gen_random_uuid()::text", script);
    }

    [Fact]
    public void SeedScript_MustEnforcePasswordResetRequirement() {
        var script = GetSeedScriptContent();

        // Garantir que a coluna password_reset_required é garantida e setada como true
        Assert.Contains("password_reset_required boolean NOT NULL DEFAULT false", script);
        Assert.Contains("password_reset_required", script);
        Assert.Contains("password_reset_required = true", script);
    }

    [Fact]
    public void SeedScript_MustContainStrictEnvironmentGuardrail() {
        var script = GetSeedScriptContent();

        // 1. Bloqueio para qualquer variação de produção
        Assert.Contains("current_database() ~* '(prod|production|live|prd)'", script);

        // 2. Nome de banco 'valora' não é evidência de segurança por si só
        Assert.Contains("current_database() ~* '^(valora|valora_db|valora_app|valora_insight)$'", script);
        Assert.Contains("valora.allow_demo_seed=true", script);

        // 3. Exige ambiente seguro explícito
        Assert.Contains("test|teste|homolog|staging|qa|dev", script);
    }

    [Theory]
    [InlineData("valora_prod", null, false)]
    [InlineData("valora_production", null, false)]
    [InlineData("production_valora", null, false)]
    [InlineData("valora_live", null, false)]
    [InlineData("valora_prd", null, false)]
    [InlineData("valora", null, false)]
    [InlineData("valora_db", null, false)]
    [InlineData("valora_app", null, false)]
    [InlineData("valora", "false", false)]
    [InlineData("valora", "true", true)]
    [InlineData("valora_homolog", null, true)]
    [InlineData("valora_staging", null, true)]
    [InlineData("valora_test", null, true)]
    [InlineData("valora_dev", null, true)]
    [InlineData("qa_valora", null, true)]
    public void Guardrail_EvaluationLogic_RejectsProductionAndEnforcesSafeEnvironments(
        string databaseName,
        string? allowDemoSeed,
        bool expectedAllowed) {
        var isAllowed = !Regex.IsMatch(databaseName, @"(prod|production|live|prd)", RegexOptions.IgnoreCase)
            && ((Regex.IsMatch(databaseName, @"^(valora|valora_db|valora_app|valora_insight)$", RegexOptions.IgnoreCase) && allowDemoSeed == "true")
                || (!Regex.IsMatch(databaseName, @"^(valora|valora_db|valora_app|valora_insight)$", RegexOptions.IgnoreCase)
                    && (Regex.IsMatch(databaseName, @"(test|teste|homolog|staging|qa|dev)", RegexOptions.IgnoreCase) || allowDemoSeed == "true")));

        Assert.Equal(expectedAllowed, isAllowed);
    }
}
