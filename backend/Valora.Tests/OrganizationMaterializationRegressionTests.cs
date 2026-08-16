using Valora.Application.ReadModels;
using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class OrganizationMaterializationRegressionTests
{
    [Fact]
    public void OrganizationProjectionIsDapperFriendlyForNpgsqlTimestamps()
    {
        Assert.NotNull(typeof(OrganizationRecord).GetConstructor(Type.EmptyTypes));
        Assert.Equal(typeof(DateTime), typeof(OrganizationRecord).GetProperty(nameof(OrganizationRecord.CreatedAt))!.PropertyType);
        Assert.Equal(typeof(DateTime?), typeof(OrganizationRecord).GetProperty(nameof(OrganizationRecord.UpdatedAt))!.PropertyType);
    }

    [Fact]
    public void OrganizationQueryUsesExplicitAliasesAndCancellableCommand()
    {
        var source = File.ReadAllText(RepositoryPaths.InfrastructureFile("Repositories", "OrganizationRepository.cs"));
        foreach (var alias in new[] { "Id", "Name", "PublicName", "Slug", "Email", "Phone", "Status", "DefaultLanguageCode", "TimeZone", "OnboardingStatus", "CreatedAt", "UpdatedAt", "Version" })
            Assert.Contains($"AS \"{alias}\"", source);

        Assert.Contains("new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken)", source);
        Assert.Contains("QuerySingleOrDefaultAsync<OrganizationRecord>", source);
    }

    [Fact]
    public void CanonicalScriptReconcilesOrganizationDefaultsBeforeNotNull()
    {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        foreach (var column in new[] { "public_name", "email", "phone", "default_language_code", "time_zone", "onboarding_status", "version" })
            Assert.Contains($"organizations ADD COLUMN IF NOT EXISTS {column}", sql);

        Assert.Contains("SET default_language_code = 'pt-BR'", sql);
        Assert.Contains("SET time_zone = 'America/Belem'", sql);
        Assert.Contains("SET onboarding_status = 'pending'", sql);
        Assert.Contains("ALTER COLUMN version SET NOT NULL", sql);
    }
}
