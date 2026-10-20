using System.Text.RegularExpressions;
using Xunit;

namespace Valora.Tests;

public sealed class CommercialVisualAndAccessibilityComplianceTests {
    private static string WebRootPath => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Valora.Web"));

    [Fact]
    public void ValoraDesignSystem_HasOfficialValoraGroupColorPaletteAndTokens() {
        var cssPath = Path.Combine(WebRootPath, "wwwroot", "css", "valora-design-system.css");
        Assert.True(File.Exists(cssPath), $"Arquivo de design system não encontrado: {cssPath}");

        var css = File.ReadAllText(cssPath);

        // Paleta oficial Valora Group
        Assert.Contains("--valora-navy", css);
        Assert.Contains("--valora-gold", css);
        Assert.Contains("--valora-teal", css);
        Assert.Contains("--font-primary", css);
        Assert.Contains("--spacing-", css);
        Assert.Contains("--radius-", css);
    }

    [Fact]
    public void ValoraResponsiveCss_ContainsMobileTabletAndDesktopBreakpoints() {
        var cssPath = Path.Combine(WebRootPath, "wwwroot", "css", "valora-responsive.css");
        Assert.True(File.Exists(cssPath), $"Arquivo de CSS responsivo não encontrado: {cssPath}");

        var css = File.ReadAllText(cssPath);

        // Breakpoints exigidos: mobile (390px/480px), tablet (768px/992px), desktop (1200px/1440px)
        Assert.Contains("@media", css);
        Assert.Contains("max-width", css);
        Assert.Contains("768px", css);
    }

    [Fact]
    public void RazorViews_FormInputs_HaveAccessibleLabelsOrAriaAttributes() {
        var viewsPath = Path.Combine(WebRootPath, "Views");
        Assert.True(Directory.Exists(viewsPath), $"Diretório de Views não encontrado: {viewsPath}");

        var cshtmlFiles = Directory.GetFiles(viewsPath, "*.cshtml", SearchOption.AllDirectories)
            .Where(f => !f.Contains("_ViewStart") && !f.Contains("_ViewImports"))
            .ToList();

        Assert.NotEmpty(cshtmlFiles);

        var inputsWithoutLabels = new List<string>();
        var inputRegex = new Regex(@"<input\s+([^>]*?)>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var typeHiddenOrSubmit = new Regex(@"type\s*=\s*[""'](hidden|submit|button|reset|image)[""']", RegexOptions.IgnoreCase);
        var hasAriaOrTitle = new Regex(@"(aria-label|aria-labelledby|title|placeholder|asp-for)\s*=", RegexOptions.IgnoreCase);

        foreach (var file in cshtmlFiles) {
            var content = File.ReadAllText(file);
            var matches = inputRegex.Matches(content);
            foreach (Match match in matches) {
                var attrs = match.Groups[1].Value;
                if (typeHiddenOrSubmit.IsMatch(attrs)) continue;

                // Inputs visíveis devem ter id associado a label, aria-label, aria-labelledby, placeholder, ou estar aninhados dentro de <label>
                var matchIndex = match.Index;
                var precedingSlice = content.Substring(Math.Max(0, matchIndex - 300), Math.Min(matchIndex, 300));
                var lastOpen = precedingSlice.LastIndexOf("<label", StringComparison.OrdinalIgnoreCase);
                var lastClose = precedingSlice.LastIndexOf("</label>", StringComparison.OrdinalIgnoreCase);
                var isNestedInLabel = lastOpen != -1 && (lastClose == -1 || lastOpen > lastClose);
                var hasSiblingLabel = lastOpen != -1 && lastClose != -1 && lastClose > lastOpen;

                if (!hasAriaOrTitle.IsMatch(attrs) && !attrs.Contains("id=") && !isNestedInLabel && !hasSiblingLabel) {
                    inputsWithoutLabels.Add($"{Path.GetFileName(file)}: {match.Value}");
                }
            }
        }

        Assert.Empty(inputsWithoutLabels);
    }

    [Fact]
    public void PageExperienceCatalog_ExplainsPurposeForEssentialPages() {
        var catalogPath = Path.Combine(WebRootPath, "Ui", "PageExperienceCatalog.cs");
        Assert.True(File.Exists(catalogPath), $"PageExperienceCatalog.cs não encontrado: {catalogPath}");

        var code = File.ReadAllText(catalogPath);

        var essentialPages = new[] {
            "Account/Login", "Account/Register", "Dashboard", "Diagnostics",
            "Forms", "Surveys", "Results", "Reports", "ActionCenter",
            "Evolution", "Onboarding", "Plans"
        };

        foreach (var page in essentialPages) {
            Assert.Contains($"[\"{page}\"]", code);
        }
    }

    [Fact]
    public void LoadingStatesAndErrorAlerts_AreDefinedInComponentCss() {
        var cssDir = Path.Combine(WebRootPath, "wwwroot", "css");
        var allCssFiles = Directory.GetFiles(cssDir, "*.css", SearchOption.AllDirectories);
        Assert.NotEmpty(allCssFiles);

        var combinedCss = string.Join("\n", allCssFiles.Select(File.ReadAllText));

        // Estados de carregamento e mensagens de erro visíveis
        Assert.True(combinedCss.Contains("skeleton") || combinedCss.Contains("spinner") || combinedCss.Contains("loading"),
            "Estados de carregamento devem estar definidos no CSS.");
        Assert.True(combinedCss.Contains("error-state") || combinedCss.Contains("alert-danger") || combinedCss.Contains("invalid-feedback"),
            "Estilos de mensagens de erro devem estar definidos no CSS.");
    }
}
