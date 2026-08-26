using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Valora.Application.Access;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

/// <summary>Typed MVC shell for tenant-safe administration. Mutations are sent to the authenticated BFF; tenant IDs are never accepted as free text.</summary>
[Authorize(Roles = "admin_valora,empresa_admin")]
[Route("Admin")]
public sealed class AdminHubController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(new AdminHubIndexViewModel([
        new("Organizações ativas", 0, "success"), new("Usuários ativos", 0), new("Unidades", 0), new("Eventos hoje", 0, "warning")]));

    [HttpGet("Organizations")]
    public IActionResult Organizations(string? search) => View(new AdminOrganizationsViewModel([], search));

    [Authorize(Roles = "admin_valora")]
    [HttpGet("Organizations/Create")]
    public IActionResult CreateOrganization() => View(new CreateOrganizationViewModel { Plans = PlanOptions() });

    [Authorize(Roles = "admin_valora")]
    [ValidateAntiForgeryToken, HttpPost("Organizations/Create")]
    public IActionResult CreateOrganization(CreateOrganizationViewModel model)
    {
        if (!ModelState.IsValid) return View(model.WithOptions(PlanOptions()));
        TempData["AdminHubCommand"] = "organization.create";
        return RedirectToAction(nameof(Organizations));
    }

    [HttpGet("Organizations/Details/{id:guid}")]
    public IActionResult OrganizationDetails(Guid id) => View(new AdminOrganizationDetailsViewModel(new(id, "Organização", "organization", "active", "Free", 0, 0), []));

    [HttpGet("Users")]
    public IActionResult Users(string? search) => View(new AdminUsersViewModel(search, []));

    [HttpGet("Users/Create")]
    public IActionResult CreateUser() => View(new CreateAdminUserViewModel());

    [ValidateAntiForgeryToken, HttpPost("Users/Create")]
    public IActionResult CreateUser(CreateAdminUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        TempData["AdminHubCommand"] = "user.create";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("Roles")]
    public IActionResult Roles() => View(new AdminRolesViewModel(ValoraPermissions.All.Order(StringComparer.Ordinal).ToArray()));

    [HttpGet("Settings")]
    public IActionResult Settings() => View(new AdminSettingsViewModel("Minha organização", true, true, true));

    [HttpGet("Audit")]
    public IActionResult Audit(string? search, DateTimeOffset? from, DateTimeOffset? to) => View(new AdminAuditViewModel(search, from, to));

    private static IReadOnlyList<SelectListItem> PlanOptions() => [new("Free", "free"), new("Start", "start"), new("Growth", "growth"), new("Enterprise", "enterprise")];
}

file static class AdminHubModelExtensions
{
    public static CreateOrganizationViewModel WithOptions(this CreateOrganizationViewModel model, IReadOnlyList<SelectListItem> plans) => new()
    { Name=model.Name, Slug=model.Slug, PlanCode=model.PlanCode, UserLimit=model.UserLimit, Plans=plans };
}
