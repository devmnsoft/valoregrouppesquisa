using Valora.Application.Experience;

namespace Valora.Tests;

public sealed class ValoraV9ExperienceTests
{
    [Fact]
    public void OfficialCatalog_ContainsTheTenCommercialTemplates()
    {
        Assert.Equal(10, OfficialTemplateCatalog.All.Count);
        Assert.Contains(OfficialTemplateCatalog.All, template => template.Name == "Diagnóstico Essencial");
        Assert.Contains(OfficialTemplateCatalog.All, template => template.Name == "Holding ou Grupo Empresarial");
        Assert.All(OfficialTemplateCatalog.All, template => Assert.InRange(template.EstimatedMinutes, 1, 480));
    }

    [Fact]
    public void OfficialCatalog_ExposesExecutiveDeliverables()
    {
        var governance = OfficialTemplateCatalog.Find("GOVERNANCA");

        Assert.NotNull(governance);
        Assert.True(governance.Report);
        Assert.True(governance.Certificate);
        Assert.True(governance.Comparison);
        Assert.Contains("Riscos", governance.Dimensions);
    }

    [Fact]
    public void OfficialCatalog_DoesNotResolveUnknownTemplate()
    {
        Assert.Null(OfficialTemplateCatalog.Find("template-de-outra-organizacao"));
    }
}
