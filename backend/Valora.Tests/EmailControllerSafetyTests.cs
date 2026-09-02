using Valora.Application.DTOs;
using Valora.Tests.Support;

namespace Valora.Tests;

public sealed class EmailControllerSafetyTests
{
    [Fact]
    public void EmailEndpointsDoNotParseAnUntrustedOrganizationClaim()
    {
        var source = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "EmailController.cs"));
        Assert.Contains("Guid.TryParse", source);
        Assert.DoesNotContain("Guid.Parse", source);
        Assert.Contains("OrganizationRequired()", source);
    }

    [Fact]
    public void TemplateOrganizationIsAlwaysTakenFromTheAuthenticatedContext()
    {
        var source = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "EmailController.cs"));
        Assert.Contains("request with { OrganizationId = organizationId }", source);
        Assert.DoesNotContain("dev@example.com", source);
    }

    [Fact]
    public void TemplateContractHasServerSideValidationMetadata()
    {
        var code = typeof(UpsertEmailTemplateRequest).GetProperty(nameof(UpsertEmailTemplateRequest.Code))!;
        var status = typeof(UpsertEmailTemplateRequest).GetProperty(nameof(UpsertEmailTemplateRequest.Status))!;
        Assert.NotEmpty(code.GetCustomAttributes(inherit: true));
        Assert.NotEmpty(status.GetCustomAttributes(inherit: true));
    }
}
