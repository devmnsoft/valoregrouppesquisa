using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Valora.Web.Models.ViewModels;

public sealed record AdminHubCardViewModel(string Label, int Value, string Tone = "primary");

public sealed record AdminHubIndexViewModel(IReadOnlyList<AdminHubCardViewModel> Cards, string ScopeLabel, string? Notice = null);

public sealed record AdminOrganizationRowViewModel(Guid Id, string Name, string Slug, string Status, string Plan, int? UsagePercent, int? Users, int? Units, DateTime CreatedAt);

public sealed record AdminOrganizationsViewModel(
    IReadOnlyList<AdminOrganizationRowViewModel> Organizations,
    string? Search = null,
    bool IsPlatformScope = false,
    int Page = 1,
    long Total = 0,
    int PageSize = 20,
    string? Error = null);

public sealed record AdminUsageRowViewModel(string Key, string Period, long Consumed, int? Limit, decimal? Percentage, bool Unlimited);

public sealed record AdminAuditRowViewModel(DateTime CreatedAt, string Severity, string Action, string? EntityType, string? EntityId, string? Message, string? UserAgent);

public sealed class OrganizationDetailsFormModel {
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Status { get; set; } = "";
    public string Plan { get; set; } = "—";
    public string? Cnpj { get; set; }
    public DateTime CreatedAt { get; set; }
    public long Version { get; set; }

    [StringLength(160)] public string? PublicName { get; set; }
    [StringLength(32)] public string? Phone { get; set; }
    [EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(320)] public string? Email { get; set; }
    [StringLength(64)] public string? TimeZone { get; set; }
    [StringLength(160)] public string? LegalName { get; set; }
    [StringLength(160)] public string? Segment { get; set; }
    [StringLength(64)] public string? Region { get; set; }
    [StringLength(128)] public string? City { get; set; }
    [StringLength(2)] public string? State { get; set; }
    [StringLength(160)] public string? PrimaryContactName { get; set; }

    public IReadOnlyList<AdminUsageRowViewModel> Usage { get; set; } = [];
    public IReadOnlyList<AdminAuditRowViewModel> RecentEvents { get; set; } = [];
    public string? Error { get; set; }
}

public sealed class CreateOrganizationViewModel {
    [Required(ErrorMessage = "Informe o nome da organização."), StringLength(160)] public string CompanyName { get; set; } = "";
    [Required(ErrorMessage = "Informe o CNPJ."), StringLength(18)] public string Cnpj { get; set; } = "";
    [StringLength(160)] public string? TradeName { get; set; }
    [Required(ErrorMessage = "Informe o nome do administrador."), StringLength(160)] public string AdministratorName { get; set; } = "";
    [Required(ErrorMessage = "Informe o e-mail do administrador."), EmailAddress, StringLength(320)] public string AdministratorEmail { get; set; } = "";
    [Required(ErrorMessage = "Defina a senha inicial."), StringLength(72), DataType(DataType.Password)] public string Password { get; set; } = "";
    [StringLength(32)] public string Phone { get; set; } = "";
    [Required] public string TimeZone { get; set; } = "America/Sao_Paulo";
    [Required(ErrorMessage = "Selecione um plano.")] public string PlanCode { get; set; } = "";
    [StringLength(160)] public string? RoleTitle { get; set; }
    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
    public string Language { get; set; } = "pt-BR";
    public IReadOnlyList<SelectListItem> Plans { get; set; } = [];
    public string? Error { get; set; }
}

public sealed record AdminUserRowViewModel(Guid Id, string Name, string Email, string? Phone, string Status, string Roles, DateTimeOffset? LastLoginAt);

public sealed record AdminUsersViewModel(
    IReadOnlyList<AdminUserRowViewModel> Users,
    string? Search = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20,
    long TotalItems = 0,
    int TotalPages = 1,
    string? OrganizationName = null,
    string? Error = null);

public sealed class CreateAdminUserViewModel {
    [Required(ErrorMessage = "Informe o nome completo."), StringLength(160)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress, StringLength(320)] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Defina a senha inicial."), StringLength(72), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required(ErrorMessage = "Selecione um papel.")] public string RoleCode { get; set; } = "";
    public string OrganizationName { get; init; } = "";
    public IReadOnlyList<SelectListItem> Roles { get; set; } = [];
    public string? Error { get; set; }
}

public sealed class EditAdminUserViewModel {
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string OrganizationName { get; init; } = "";
    [Required(ErrorMessage = "Informe o nome completo."), StringLength(160)] public string Name { get; set; } = "";
    [StringLength(32)] public string? Phone { get; set; }
    [Required(ErrorMessage = "Selecione um status.")] public string Status { get; set; } = "active";
    public string[] RoleCodes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Roles { get; set; } = [];
    public string? Error { get; set; }
}

public sealed record AdminRoleRowViewModel(Guid Id, string Code, string Name, string? Description, bool IsSystem, string Status, int UserCount, int PermissionCount);

public sealed record AdminModuleRowViewModel(string Code, string Name, string Status, int PermissionCount);

public sealed record AdminRolesViewModel(
    IReadOnlyList<AdminRoleRowViewModel> Roles,
    IReadOnlyList<AdminModuleRowViewModel> Modules,
    string? OrganizationName = null,
    string? Error = null);

public sealed class AdminSettingsFormModel {
    public string OrganizationName { get; init; } = "";
    public string Plan { get; init; } = "—";
    public bool DiagnosticReminders { get; set; }
    public bool ReportNotifications { get; set; }
    public bool AnonymizeResponses { get; set; }
    public IReadOnlyList<AdminUsageRowViewModel> Usage { get; init; } = [];
    public string? Error { get; set; }
}

public sealed record AdminAuditPageViewModel(
    IReadOnlyList<AdminAuditRowViewModel> Events,
    int LoadedTotal = 0,
    string? Search = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? OrganizationName = null,
    string? Error = null);
