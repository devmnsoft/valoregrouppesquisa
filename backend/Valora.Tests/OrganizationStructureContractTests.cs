using System.IO;
using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

public sealed class OrganizationStructureContractTests {
    [Fact]
    public void Api_exposes_unit_and_department_lifecycle_endpoints() {
        var controller = File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "OrganizationStructureController.cs"));
        Assert.Contains("/api/v1/units", controller);
        Assert.Contains("/api/v1/units/{id:guid}/deactivate", controller);
        Assert.Contains("/api/v1/units/{id:guid}/reactivate", controller);
        Assert.Contains("/api/v1/departments", controller);
        Assert.Contains("/api/v1/departments/{id:guid}/deactivate", controller);
        Assert.Contains("/api/v1/departments/{id:guid}/reactivate", controller);
        Assert.Contains("ValoraPermissions.Units.Read", controller);
        Assert.Contains("ValoraPermissions.Departments.Disable", controller);
        Assert.Contains("Guid.TryParse", controller);
    }

    [Fact]
    public void Structure_service_blocks_creation_with_friendly_plan_message() {
        var service = File.ReadAllText(RepositoryPaths.ApplicationFile("Services", "OrganizationStructureService.cs"));
        Assert.Contains("Seu plano atual não permite esta ação", service);
        Assert.Contains("CheckLimitAsync(organizationId, \"units\", 1)", service);
        Assert.Contains("CheckLimitAsync(organizationId, \"departments\", 1)", service);
        Assert.Contains("ValidateUnitScopeAsync", service);
        Assert.Contains("não pertence à sua empresa", service);
        Assert.Contains("department.deactivated", service);
    }

    [Fact]
    public void Organization_page_is_connected_to_structure_bff_actions() {
        var api = File.ReadAllText(RepositoryPaths.WebFile("wwwroot", "js", "api", "organization-api.js"));
        var page = File.ReadAllText(RepositoryPaths.WebFile("wwwroot", "js", "pages", "organization-page.js"));
        Assert.Contains("/bff/units", api);
        Assert.Contains("/bff/departments", api);
        Assert.Contains("data-add-unit", page);
        Assert.Contains("Confirma alterar o status", page);
        Assert.Contains("data-structure-status", File.ReadAllText(RepositoryPaths.WebFile("Views", "Organization", "Index.cshtml")));
        Assert.Contains("unitId", page);
    }
}
