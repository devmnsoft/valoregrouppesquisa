using System.IO;
using Xunit;

namespace Valora.Tests;

public sealed class OrganizationStructureContractTests
{
    [Fact]
    public void Api_exposes_unit_and_department_lifecycle_endpoints()
    {
        var controller = File.ReadAllText(Path.Combine("..", "..", "..", "..", "backend", "Valora.Api", "Controllers", "OrganizationStructureController.cs"));
        Assert.Contains("/api/v1/units", controller);
        Assert.Contains("/api/v1/units/{id:guid}/deactivate", controller);
        Assert.Contains("/api/v1/units/{id:guid}/reactivate", controller);
        Assert.Contains("/api/v1/departments", controller);
        Assert.Contains("/api/v1/departments/{id:guid}/deactivate", controller);
        Assert.Contains("/api/v1/departments/{id:guid}/reactivate", controller);
    }

    [Fact]
    public void Structure_service_blocks_creation_with_friendly_plan_message()
    {
        var service = File.ReadAllText(Path.Combine("..", "..", "..", "..", "backend", "Valora.Application", "Services", "OrganizationStructureService.cs"));
        Assert.Contains("Seu plano atual não permite esta ação", service);
        Assert.Contains("CheckLimitAsync(organizationId, \"units\", 1)", service);
        Assert.Contains("CheckLimitAsync(organizationId, \"departments\", 1)", service);
    }

    [Fact]
    public void Organization_page_is_connected_to_structure_bff_actions()
    {
        var api = File.ReadAllText(Path.Combine("..", "..", "..", "..", "backend", "Valora.Web", "wwwroot", "js", "api", "organization-api.js"));
        var page = File.ReadAllText(Path.Combine("..", "..", "..", "..", "backend", "Valora.Web", "wwwroot", "js", "pages", "organization-page.js"));
        Assert.Contains("/bff/units", api);
        Assert.Contains("/bff/departments", api);
        Assert.Contains("data-add-unit", page);
        Assert.Contains("Confirma alterar o status", page);
    }
}
