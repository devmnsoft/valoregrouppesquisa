using Valora.Application.Access;

namespace Valora.Tests;

public sealed class PlatformAccessAndShellContractTests
{
    [Fact]
    public void PlatformCatalogMapsEveryPermissionWithoutPrefixGuessing()
    {
        var capabilities = ValoraAccessCatalog.CapabilitiesFor(ValoraPermissions.All);
        Assert.NotEmpty(capabilities);
        Assert.Contains("identity", capabilities);
        Assert.Contains("operations", capabilities);
        Assert.Contains("organizational_intelligence", ValoraAccessCatalog.PlatformModules);
        Assert.Equal(ValoraAccessCatalog.PlatformRole, "admin_valora");
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
