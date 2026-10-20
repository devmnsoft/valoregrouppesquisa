using Xunit;

namespace Valora.Tests;

public class DesignSystemV2Tests {
    private static readonly string WwwrootCssPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Valora.Web", "wwwroot", "css"));

    [Fact]
    public void DesignSystem_Defines_Refined_Enterprise_Geometry_Tokens() {
        var path = Path.Combine(WwwrootCssPath, "valora-design-system.css");
        Assert.True(File.Exists(path), $"Arquivo CSS não encontrado: {path}");

        var content = File.ReadAllText(path);

        // Assert presence of modern enterprise radius tokens
        Assert.Contains("--valora-radius-xs: 4px;", content);
        Assert.Contains("--valora-radius-sm: 6px;", content);
        Assert.Contains("--valora-radius-md: 8px;", content);
        Assert.Contains("--valora-radius: 10px;", content);
        Assert.Contains("--valora-radius-lg: 12px;", content);

        // Assert absence of toy-like 22px card radius
        Assert.DoesNotContain("--valora-radius:22px;", content);
        Assert.DoesNotContain("--valora-radius: 22px;", content);
    }

    [Fact]
    public void DesignSystem_Defines_Layered_Elevation_Shadows() {
        var path = Path.Combine(WwwrootCssPath, "valora-design-system.css");
        var content = File.ReadAllText(path);

        Assert.Contains("--valora-shadow-xs:", content);
        Assert.Contains("--valora-shadow-sm:", content);
        Assert.Contains("--valora-shadow-soft:", content);
        Assert.Contains("--valora-shadow:", content);
        Assert.Contains("--valora-shadow-lg:", content);

        // Assert that giant murky shadow 0 24px 70px is eliminated
        Assert.DoesNotContain("0 24px 70px", content);
    }

    [Fact]
    public void AppCss_Eliminates_Inflated_Card_Radii() {
        var path = Path.Combine(WwwrootCssPath, "app.css");
        Assert.True(File.Exists(path), $"Arquivo CSS não encontrado: {path}");

        var content = File.ReadAllText(path);

        // Verify page-card uses token or balanced radius
        Assert.Contains(".page-card{background:#fff;border-radius:var(--valora-radius,10px)", content);
        Assert.Contains(".card{border:1px solid var(--valora-border,#e2e8f0)", content);
        Assert.DoesNotContain("page-card{background:#fff;border-radius:24px", content);
    }

    [Fact]
    public void ValoraButtons_Uses_Enterprise_Radius() {
        var path = Path.Combine(WwwrootCssPath, "valora-buttons.css");
        Assert.True(File.Exists(path), $"Arquivo CSS não encontrado: {path}");

        var content = File.ReadAllText(path);

        Assert.Contains("border-radius: var(--valora-radius-sm, 6px);", content);
        Assert.Contains("min-height: var(--valora-button-height, 40px);", content);
    }

    [Fact]
    public void ValoraTables_And_Modals_Are_Harmonized_With_Tokens() {
        var tablesPath = Path.Combine(WwwrootCssPath, "valora-tables.css");
        var modalsPath = Path.Combine(WwwrootCssPath, "valora-modals.css");

        var tablesContent = File.ReadAllText(tablesPath);
        var modalsContent = File.ReadAllText(modalsPath);

        Assert.Contains("border-radius: var(--valora-radius, 10px);", tablesContent);
        Assert.Contains("border-radius: var(--valora-radius-lg, 12px);", modalsContent);
    }
}
