using Valora.Application.Access;
using System.Text.RegularExpressions;

namespace Valora.Tests;

public sealed class PlatformAccessAndShellContractTests
{
    [Fact]
    public void PlatformCatalogMapsEveryPermissionWithoutPrefixGuessing()
    {
        var capabilities = ValoraAccessCatalog.CapabilitiesForStrict(ValoraPermissions.All);
        Assert.NotEmpty(capabilities);
        Assert.Contains("identity", capabilities);
        Assert.Contains("operations", capabilities);
        Assert.Contains("organizational_intelligence", ValoraAccessCatalog.PlatformModules);
        Assert.Equal(ValoraAccessCatalog.PlatformRole, "admin_valora");
    }

    [Fact]
    public void UnitsIsAnOfficialOrganizationPermission()
    {
        Assert.Equal("organization", ValoraAccessCatalog.PermissionCapability(ValoraPermissions.Units.Read));
        Assert.Equal(new[] { "organization" }, ValoraAccessCatalog.CapabilitiesForStrict(["units.read"]));
    }

    [Fact]
    public void RuntimeResolutionDeniesUnknownPermissionWithoutBreakingLogin()
    {
        var warnings = new List<string>();
        var capabilities = ValoraAccessCatalog.CapabilitiesFor(["units.read", "legacy.unknown"], warnings.Add);
        Assert.Equal(new[] { "organization" }, capabilities);
        Assert.Equal(new[] { "legacy.unknown" }, warnings);
    }

    [Fact]
    public void EveryPermissionSeededByTheCompleteDatabaseScriptIsCanonical()
    {
        var root = FindRepositoryRoot();
        var sql = File.ReadAllText(Path.Combine(root, "backend/database/postgresql/script_completo.sql"));
        var insertStatements = Regex.Matches(sql, @"INSERT INTO valorapesquisa\.permissions\([^;]+?;",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var seeded = insertStatements.SelectMany(statement =>
                Regex.Matches(statement.Value, @"\('([a-z_]+(?:\.[a-z_]+)+)'").Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var unknown = seeded.Where(permission => !ValoraAccessCatalog.IsCanonicalPermission(permission)).ToArray();
        Assert.NotEmpty(seeded);
        Assert.Empty(unknown);
    }

    [Fact]
    public void AdminValoraLoginPathUsesCompleteCatalogAndDoesNotDependOnTenantAccessService()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "backend/Valora.Application/Services/Auth/AuthService.cs"));
        Assert.Contains("roles.Contains(ValoraAccessCatalog.PlatformRole", source);
        Assert.Contains("var permissions = ValoraPermissions.All", source);
        Assert.Contains("ValoraAccessCatalog.PlatformModules", source);
        Assert.Contains("ResolveCapabilitiesSafely(permissions)", source);
        Assert.DoesNotContain("Split(\".\")", source);
        Assert.DoesNotContain("Split('.')", source);
    }

    [Fact]
    public void AdministrativeLayoutLoadsCanonicalHiddenRuleAndAdminModulesInOrder()
    {
        var root = FindRepositoryRoot();
        var layout = File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Views/Shared/_AdminLayout.cshtml"));
        Assert.True(layout.IndexOf("design-system/components.css", StringComparison.Ordinal) < layout.IndexOf("valora-admin.css", StringComparison.Ordinal));
        Assert.True(layout.IndexOf("valora-admin.css", StringComparison.Ordinal) < layout.IndexOf("design-system/responsive.css", StringComparison.Ordinal));
        Assert.Contains("[hidden]", File.ReadAllText(Path.Combine(root, "backend/Valora.Web/wwwroot/css/design-system/tokens.css")));
        Assert.DoesNotContain("quick-action", File.ReadAllText(Path.Combine(root, "backend/Valora.Web/Views/Shared/_Topbar.cshtml")));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend/Valora.Web"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
