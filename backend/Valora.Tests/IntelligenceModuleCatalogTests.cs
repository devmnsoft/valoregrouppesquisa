extern alias ValoraWeb;

using IntelligenceModuleViewModel = ValoraWeb::Valora.Web.Models.ViewModels.IntelligenceModuleViewModel;

namespace Valora.Tests;

public sealed class IntelligenceModuleCatalogTests
{
    [Theory]
    [InlineData("dashboard")]
    [InlineData("metrics")]
    [InlineData("radar")]
    [InlineData("heatmap")]
    [InlineData("benchmark")]
    [InlineData("insights")]
    [InlineData("action")]
    [InlineData("evolution")]
    [InlineData("journey")]
    [InlineData("executive-report")]
    [InlineData("one-on-one")]
    [InlineData("power-bi")]
    public void Find_ReturnsEveryProfessionalModule(string slug)
    {
        var module = IntelligenceModuleViewModel.Find(slug);

        Assert.NotNull(module);
        Assert.NotEmpty(module.Capabilities);
        Assert.False(string.IsNullOrWhiteSpace(module.ApiResource));
        Assert.Contains("dados suficientes", module.EmptyMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Find_RejectsUnknownModule() => Assert.Null(IntelligenceModuleViewModel.Find("unknown"));
}
