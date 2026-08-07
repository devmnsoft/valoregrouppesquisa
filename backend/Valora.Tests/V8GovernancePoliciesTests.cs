using Valora.Domain.Operations;

namespace Valora.Tests;

public sealed class V8GovernancePoliciesTests
{
    [Fact]
    public void Anonymous_segment_requires_minimum_and_never_exposes_individual()
    {
        Assert.False(AnonymityPolicy.CanExposeSegment(true, 4, 5));
        Assert.True(AnonymityPolicy.CanExposeSegment(true, 5, 5));
        Assert.False(AnonymityPolicy.CanExposeIndividual(true));
        Assert.Equal("Não há respostas suficientes para exibir este recorte sem comprometer o anonimato.", AnonymityPolicy.InsufficientDataMessage);
    }

    [Fact]
    public void Minimum_anonymity_floor_cannot_be_lower_than_three()
    {
        Assert.False(AnonymityPolicy.CanExposeSegment(true, 2, 1));
        Assert.True(AnonymityPolicy.CanExposeSegment(true, 3, 1));
    }

    [Fact]
    public void Backup_status_reflects_provider_evidence_without_simulating_backup()
    {
        var now = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal("not_configured", BackupFreshnessPolicy.Resolve(null, 24, false, now));
        Assert.Equal("failed", BackupFreshnessPolicy.Resolve(now.AddHours(-1), 24, true, now));
        Assert.Equal("delayed", BackupFreshnessPolicy.Resolve(now.AddHours(-25), 24, false, now));
        Assert.Equal("current", BackupFreshnessPolicy.Resolve(now.AddHours(-23), 24, false, now));
    }

    [Fact]
    public void Migration_validation_rejects_invalid_company_and_orphan_response()
    {
        var company = MigrationSafetyPolicy.Validate("companies", new Dictionary<string, string> { ["nome"] = "Cliente", ["cnpj"] = "123" });
        var response = MigrationSafetyPolicy.Validate("responses", new Dictionary<string, string> { ["nome"] = "Resposta" });
        Assert.Contains("CNPJ deve conter 14 dígitos.", company);
        Assert.Contains("Pesquisa é obrigatória para respostas.", response);
    }

    [Fact]
    public void Operational_catalogs_cover_full_go_live()
    {
        Assert.Equal(15, GovernanceCatalog.ImplementationSteps.Length);
        Assert.Equal(21, GovernanceCatalog.ProductionChecklist.Length);
        Assert.Equal("Go-live", GovernanceCatalog.ImplementationSteps[^1]);
        Assert.Contains("LGPD configurada", GovernanceCatalog.ProductionChecklist);
    }
}
