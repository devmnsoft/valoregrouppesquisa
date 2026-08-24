extern alias ValoraWeb;
using Microsoft.AspNetCore.Authorization;
using HomeController = ValoraWeb::Valora.Web.Controllers.HomeController;
using PlansController = ValoraWeb::Valora.Web.Controllers.PlansController;
using PublicPagesController = ValoraWeb::Valora.Web.Controllers.PublicPagesController;
using AccountController = ValoraWeb::Valora.Web.Controllers.AccountController;
using EnterpriseController = ValoraWeb::Valora.Web.Controllers.EnterpriseController;

namespace Valora.Tests;

public sealed class PublicPortalContractTests
{
    [Theory]
    [InlineData(typeof(HomeController))]
    [InlineData(typeof(PlansController))]
    [InlineData(typeof(PublicPagesController))]
    [InlineData(typeof(AccountController))]
    public void Public_page_controllers_are_explicitly_anonymous(Type controller) =>
        Assert.NotNull(controller.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());

    [Fact]
    public void Administrative_portal_remains_role_protected()
    {
        var authorization = typeof(EnterpriseController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Equal("admin_valora", authorization.Roles);
    }

    [Theory]
    [InlineData("Views/PublicPages/About.cshtml")]
    [InlineData("Views/PublicPages/Methodology.cshtml")]
    [InlineData("Views/PublicPages/Demo.cshtml")]
    [InlineData("Views/PublicPages/Terms.cshtml")]
    public void Required_commercial_views_are_deployed(string relativePath) =>
        Assert.True(File.Exists(Path.Combine(RepositoryRoot(), "backend/Valora.Web", relativePath)), relativePath);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
