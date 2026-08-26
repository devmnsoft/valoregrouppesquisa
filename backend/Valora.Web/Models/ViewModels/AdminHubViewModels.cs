using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Valora.Web.Models.ViewModels;

public sealed record AdminHubCardViewModel(string Label, int Value, string Tone = "primary");
public sealed record AdminHubIndexViewModel(IReadOnlyList<AdminHubCardViewModel> Cards);
public sealed record AdminOrganizationRowViewModel(Guid Id, string Name, string Slug, string Status, string Plan, int Users, int Units);
public sealed record AdminOrganizationsViewModel(IReadOnlyList<AdminOrganizationRowViewModel> Organizations, string? Search = null);
public sealed record AdminOrganizationDetailsViewModel(AdminOrganizationRowViewModel Organization, IReadOnlyList<string> RecentEvents);

public sealed class CreateOrganizationViewModel
{
    [Required(ErrorMessage = "Informe o nome da organização."), StringLength(160)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Informe o identificador público."), RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Use apenas letras minúsculas, números e hífens.")] public string Slug { get; set; } = "";
    [Required(ErrorMessage = "Selecione um plano.")] public string PlanCode { get; set; } = "";
    [Range(1, 100000, ErrorMessage = "O limite deve estar entre 1 e 100.000.")] public int UserLimit { get; set; } = 10;
    public IReadOnlyList<SelectListItem> Plans { get; init; } = [];
}

public sealed class CreateAdminUserViewModel
{
    [Required, StringLength(160)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(320)] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Selecione uma organização.")] public Guid? OrganizationId { get; set; }
    public Guid? UnitId { get; set; }
    [Required(ErrorMessage = "Selecione um papel.")] public Guid? RoleId { get; set; }
    public IReadOnlyList<SelectListItem> Organizations { get; init; } = [];
    public IReadOnlyList<SelectListItem> Units { get; init; } = [];
    public IReadOnlyList<SelectListItem> Roles { get; init; } = [];
}

public sealed record AdminUsersViewModel(string? Search, IReadOnlyList<SelectListItem> Organizations);
public sealed record AdminRolesViewModel(IReadOnlyList<string> CanonicalPermissions);
public sealed record AdminSettingsViewModel(string OrganizationName, bool DiagnosticReminders, bool ReportNotifications, bool AnonymizeResponses);
public sealed record AdminAuditViewModel(string? Search, DateTimeOffset? From, DateTimeOffset? To);
