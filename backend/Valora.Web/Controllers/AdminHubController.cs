using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using Valora.Application.Access;
using Valora.Application.CompanyRegistration;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Enterprise;
using Valora.Application.Exceptions;
using Valora.Application.ReadModels;
using Valora.Domain.ValueObjects;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

/// <summary>Typed MVC hub for tenant-safe administration. Mutations run server-side against the effective organization; tenant IDs are never accepted as free text.</summary>
[Authorize(Roles = "admin_valora,empresa_admin")]
[Route("Admin")]
public sealed class AdminHubController(
    ICurrentRequestContext requestContext,
    EnterpriseService enterprise,
    RegisterCompanyHandler companyRegistration,
    IOrganizationRepository organizations,
    IUserAdministrationService userAdministration,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IPasswordPolicy passwordPolicy,
    IAccessAdministrationService access,
    IPlanRepository plans,
    IOrganizationStructureRepository structure,
    IAuditRepository audit) : Controller {
    private static readonly string[] UserStatuses = ["active", "inactive", "suspended", "locked"];

    private CurrentRequestContext Context => requestContext.GetCurrent();
    private Guid? EffectiveOrg => Context.EffectiveOrganizationId;
    private Guid Actor => Context.UserId ?? Guid.Empty;

    [HttpGet("")]
    public async Task<IActionResult> Index() {
        if (Context.IsGlobalAdministrator && EffectiveOrg is null) {
            var scope = "Escopo: plataforma (todas as organizações)";
            try {
                var summary = await enterprise.SummaryAsync(CancellationToken.None);
                return View(new AdminHubIndexViewModel([
                    new("Organizações", summary.Companies),
                    new("Em risco", summary.AtRisk, "warning"),
                    new("Trials encerrando", summary.TrialsEnding, "warning"),
                    new("Leads ativos", summary.ActiveLeads)], scope));
            }
            catch (Exception) {
                return View(new AdminHubIndexViewModel([], scope, "Não foi possível carregar os indicadores da plataforma. Os dados podem estar indisponíveis; tente novamente."));
            }
        }
        if (EffectiveOrg is not { } org)
            return View(new AdminHubIndexViewModel([], "Escopo: nenhuma organização selecionada", "Selecione uma organização no menu superior para ver os indicadores."));
        var orgName = (await organizations.GetAsync(org))?.Name ?? "organização";
        try {
            var people = await userAdministration.ListAsync(org, new UserListQuery(Page: 1, PageSize: 1));
            var managers = await organizations.CountManagersAsync(org);
            var units = (await structure.ListUnitsAsync(org)).Count;
            var roles = (await access.ListRolesAsync(org, CancellationToken.None)).Count;
            return View(new AdminHubIndexViewModel([
                new("Usuários", (int)people.TotalItems),
                new("Gestores ativos", managers),
                new("Unidades", units),
                new("Papéis disponíveis", roles)], $"Escopo: {orgName}"));
        }
        catch (Exception) {
            return View(new AdminHubIndexViewModel([], $"Escopo: {orgName}", "Não foi possível carregar os indicadores da organização. Os dados podem estar indisponíveis; tente novamente."));
        }
    }

    [HttpGet("Organizations")]
    public async Task<IActionResult> Organizations(string? search, int page = 1) {
        page = Math.Max(1, page);
        if (Context.IsGlobalAdministrator && EffectiveOrg is null) {
            try {
                var paged = await enterprise.CompaniesAsync(new EnterpriseListQuery(Search: string.IsNullOrWhiteSpace(search) ? null : search.Trim(), Status: null, Plan: null, Health: null, From: null, To: null, Page: page, PageSize: 20), CancellationToken.None);
                var rows = paged.Items.Select(c => new AdminOrganizationRowViewModel(c.Id, c.Name, string.Empty, c.Status, c.PlanName ?? c.PlanCode ?? "—", c.UsagePercent, null, null, c.CreatedAt)).ToList();
                return View(new AdminOrganizationsViewModel(rows, search, true, page, paged.Total, 20));
            }
            catch (Exception) {
                return View(new AdminOrganizationsViewModel([], search, true, page, 0, 20, "Não foi possível carregar o portfólio de organizações. Tente novamente."));
            }
        }
        if (EffectiveOrg is not { } org)
            return View(new AdminOrganizationsViewModel([], search, false, 1, 0, 20, "Selecione uma organização no menu superior."));
        try {
            var record = await organizations.GetAsync(org);
            if (record is null)
                return View(new AdminOrganizationsViewModel([], search, false, 1, 0, 20, "Organização não encontrada para o contexto atual."));
            var usage = (await organizations.GetUsageAsync(org)).Where(u => u.Percentage.HasValue).Select(u => u.Percentage!.Value).ToList();
            var usersTotal = (await userAdministration.ListAsync(org, new UserListQuery(Page: 1, PageSize: 1))).TotalItems;
            var units = (await structure.ListUnitsAsync(org)).Count;
            var row = new AdminOrganizationRowViewModel(record.Id, record.Name, record.Slug, record.Status, await PlanNameAsync(org),
                usage.Count > 0 ? (int)Math.Round(usage.Max()) : null, (int)usersTotal, units, record.CreatedAt);
            return View(new AdminOrganizationsViewModel([row], search, false, 1, 1, 20));
        }
        catch (Exception) {
            return View(new AdminOrganizationsViewModel([], search, false, 1, 0, 20, "Não foi possível carregar os dados da organização. Tente novamente."));
        }
    }

    [Authorize(Roles = "admin_valora")]
    [HttpGet("Organizations/Create")]
    public async Task<IActionResult> CreateOrganization() => View(new CreateOrganizationViewModel { Plans = await PlanOptionsAsync() });

    [Authorize(Roles = "admin_valora")]
    [ValidateAntiForgeryToken]
    [HttpPost("Organizations/Create")]
    public async Task<IActionResult> CreateOrganization(CreateOrganizationViewModel model) {
        model.Plans = await PlanOptionsAsync();
        if (!model.AcceptedTerms) ModelState.AddModelError(nameof(model.AcceptedTerms), "É necessário aceitar os termos de uso.");
        if (!model.AcceptedPrivacyPolicy) ModelState.AddModelError(nameof(model.AcceptedPrivacyPolicy), "É necessário aceitar a política de privacidade.");
        if (!Cnpj.TryCreate(model.Cnpj, out _)) ModelState.AddModelError(nameof(model.Cnpj), "CNPJ inválido.");
        var passwordCheck = passwordPolicy.Validate(model.Password, model.AdministratorEmail, model.CompanyName);
        foreach (var problem in passwordCheck.Errors) ModelState.AddModelError(nameof(model.Password), problem);
        if (!ModelState.IsValid) return View(model);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try {
            var result = await companyRegistration.HandleAsync(new RegisterCompanyRequest(
                Cnpj: model.Cnpj,
                CompanyName: model.CompanyName.Trim(),
                TradeName: string.IsNullOrWhiteSpace(model.TradeName) ? null : model.TradeName.Trim(),
                AdministratorName: model.AdministratorName.Trim(),
                AdministratorEmail: model.AdministratorEmail.Trim(),
                Password: model.Password,
                Phone: model.Phone,
                Language: string.IsNullOrWhiteSpace(model.Language) ? "pt-BR" : model.Language,
                TimeZone: string.IsNullOrWhiteSpace(model.TimeZone) ? "America/Sao_Paulo" : model.TimeZone,
                AcceptedTerms: model.AcceptedTerms,
                AcceptedPrivacyPolicy: model.AcceptedPrivacyPolicy,
                IdempotencyKey: Guid.NewGuid().ToString("N"),
                PlanCode: model.PlanCode,
                RoleTitle: string.IsNullOrWhiteSpace(model.RoleTitle) ? null : model.RoleTitle.Trim()), ip);
            await audit.AddAsync(new AuditEntry(result.OrganizationId, result.UserId, "admin.organization.created", "organization", result.OrganizationId.ToString(),
                result.Replayed ? "Organização criada (replay idempotente de tentativa anterior)." : "Organização criada pelo Admin Hub.", module: "admin"));
            TempData["AdminNotice"] = result.Replayed ? "Tentativa anterior já registrada; operação reproduzida sem duplicar dados." : "Organização criada com sucesso.";
            return RedirectToAction(nameof(Organizations));
        }
        catch (ArgumentException ex) {
            model.Error = ex.Message;
            return View(model);
        }
        catch (InvalidOperationException ex) {
            model.Error = ex.Message;
            return View(model);
        }
        catch (PostgresException ex) when (ex.SqlState is "23505" or "23503") {
            model.Error = "Não foi possível concluir o cadastro agora (registro em conflito). Tente novamente.";
            return View(model);
        }
        catch (Exception) {
            model.Error = "Não foi possível criar a organização agora. Verifique a conexão e tente novamente.";
            return View(model);
        }
    }

    [HttpGet("Organizations/Details/{id:guid}")]
    public async Task<IActionResult> OrganizationDetails(Guid id) {
        var org = await organizations.GetAsync(id);
        if (org is null || (!Context.IsGlobalAdministrator && EffectiveOrg != id))
            return View(new OrganizationDetailsFormModel { Id = id, Name = "Organização", Error = "Organização não encontrada ou fora do seu escopo de acesso." });
        var model = await HydrateDetailsAsync(id);
        return View(model);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Organizations/{id:guid}/Update")]
    public async Task<IActionResult> OrganizationDetailsUpdate(Guid id, OrganizationDetailsFormModel model) {
        if (EffectiveOrg == id || Context.IsGlobalAdministrator) {
            var current = await organizations.GetAsync(id);
            if (current is not null) {
                model.Id = id;
                model.Name = current.Name;
                model.Slug = current.Slug;
                model.Status = current.Status;
                model.Plan = await PlanNameAsync(id);
                model.Cnpj = current.Cnpj;
                model.CreatedAt = current.CreatedAt;
                if (ModelState.IsValid) {
                    var request = new UpdateOrganizationRequest(model.PublicName, model.Phone,
                        Email: model.Email, TimeZone: model.TimeZone, ExpectedVersion: model.Version,
                        LegalName: model.LegalName, Segment: model.Segment, Region: model.Region, City: model.City,
                        State: model.State, PrimaryContactName: model.PrimaryContactName);
                    try {
                        var version = await organizations.UpdateCurrentAsync(id, request);
                        if (version is null) {
                            model.Error = "Outra alteração foi registrada durante sua edição. Recarregue a página e revise os valores antes de salvar.";
                            model.Usage = await UsageRowsAsync(id);
                            model.RecentEvents = await RecentAuditAsync(id);
                            return View("OrganizationDetails", model);
                        }
                        await audit.AddAsync(new AuditEntry(id, Actor, "admin.organization.updated", "organization", id.ToString(), "Dados da organização atualizados pelo Admin Hub.", module: "admin"));
                        TempData["AdminNotice"] = "Dados da organização atualizados.";
                        return RedirectToAction("OrganizationDetails", new { id });
                    }
                    catch (PostgresException ex) when (ex.SqlState is "23505" or "23503") {
                        model.Error = "Não foi possível salvar agora (registro em conflito). Tente novamente.";
                        return View("OrganizationDetails", model);
                    }
                    catch (Exception) {
                        model.Error = "Não foi possível salvar as alterações agora. Tente novamente.";
                        return View("OrganizationDetails", model);
                    }
                }
            }
        }
        model ??= new OrganizationDetailsFormModel();
        model.Id = id;
        model.Name = model.Name is not "" ? model.Name : "Organização";
        model.Error = model.Error ?? "Organização não encontrada ou fora do seu escopo de acesso.";
        return View("OrganizationDetails", model);
    }

    [HttpGet("Users")]
    public async Task<IActionResult> Users(string? search, string? status, int page = 1, int pageSize = 20) {
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (EffectiveOrg is not { } org)
            return View(new AdminUsersViewModel([], search, status, 1, pageSize, 0, 1, null, "Selecione uma organização no menu superior para ver seus usuários."));
        page = Math.Max(1, page);
        if (status is not null && UserStatuses.All(s => !s.Equals(status, StringComparison.OrdinalIgnoreCase))) status = null;
        try {
            var paged = await userAdministration.ListAsync(org, new UserListQuery(Page: page, PageSize: pageSize, Search: string.IsNullOrWhiteSpace(search) ? null : search.Trim(), Status: status));
            var orgName = (await organizations.GetAsync(org))?.Name;
            var rows = paged.Items.Select(u => new AdminUserRowViewModel(u.Id, u.Name, u.Email, u.Phone, u.Status, string.Join(", ", u.RoleCodes), u.LastLoginAt)).ToList();
            return View(new AdminUsersViewModel(rows, search, status, paged.Page, paged.PageSize, paged.TotalItems, paged.TotalPages, orgName));
        }
        catch (Exception) {
            return View(new AdminUsersViewModel([], search, status, page, pageSize, 0, 1, null, "Não foi possível carregar os usuários. Tente novamente."));
        }
    }

    [HttpGet("Users/Create")]
    public async Task<IActionResult> CreateUser() {
        if (EffectiveOrg is not { } org)
            return View(new CreateAdminUserViewModel { OrganizationName = "", Roles = [], Error = "Selecione uma organização no menu superior antes de criar um usuário." });
        try {
            var options = await RoleOptionsAsync(org);
            return View(new CreateAdminUserViewModel { OrganizationName = (await organizations.GetAsync(org))?.Name ?? "organização", Roles = options });
        }
        catch (Exception) {
            return View(new CreateAdminUserViewModel { OrganizationName = "organização", Roles = [], Error = "Não foi possível carregar os papéis disponíveis. Tente novamente." });
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Users/Create")]
    public async Task<IActionResult> CreateUser(CreateAdminUserViewModel model) {
        if (EffectiveOrg is not { } org) {
            model.Error = "Selecione uma organização no menu superior antes de criar um usuário.";
            return View(model);
        }
        if (!ModelState.IsValid) return View(await WithRoleOptions(model, org));
        var passwordCheck = passwordPolicy.Validate(model.Password, model.Email, null);
        foreach (var problem in passwordCheck.Errors) ModelState.AddModelError(nameof(model.Password), problem);
        if (!ModelState.IsValid) return View(await WithRoleOptions(model, org));
        try {
            var userId = await users.CreateAsync(org, model.Name.Trim(), model.Email.Trim().ToLowerInvariant(), passwordHasher.Hash(model.Password), model.RoleCode);
            await audit.AddAsync(new AuditEntry(org, Actor, "admin.user.created", "user", userId.ToString(), $"Usuário {model.Email} criado com papel {model.RoleCode}.", module: "admin"));
            TempData["AdminNotice"] = "Usuário criado com sucesso.";
            return RedirectToAction(nameof(Users));
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") {
            ModelState.AddModelError(nameof(model.Email), "Já existe um usuário com este e-mail nesta organização.");
            return View(await WithRoleOptions(model, org));
        }
        catch (InvalidOperationException) {
            ModelState.AddModelError(nameof(model.RoleCode), "Papel indisponível nesta organização; selecione outro.");
            return View(await WithRoleOptions(model, org));
        }
        catch (Exception) {
            model.Error = "Não foi possível criar o usuário agora. Tente novamente.";
            return View(model);
        }
    }

    [HttpGet("Users/{id:guid}/Edit")]
    public async Task<IActionResult> UserEdit(Guid id) {
        if (EffectiveOrg is not { } org)
            return View(new EditAdminUserViewModel { Id = id, Error = "Selecione uma organização no menu superior para gerenciar usuários." });
        try {
            var user = await userAdministration.GetAsync(org, id);
            return View(new EditAdminUserViewModel {
                Id = user.Id,
                Email = user.Email,
                OrganizationName = (await organizations.GetAsync(org))?.Name ?? "organização",
                Name = user.Name,
                Phone = user.Phone,
                Status = user.Status,
                RoleCodes = [.. user.RoleCodes],
                Roles = await RoleOptionsAsync(org)
            });
        }
        catch (NotFoundAppException) {
            return View(new EditAdminUserViewModel { Id = id, Error = "Usuário não encontrado ou fora do seu escopo de acesso." });
        }
        catch (Exception) {
            return View(new EditAdminUserViewModel { Id = id, Error = "Não foi possível carregar o usuário. Tente novamente." });
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Users/{id:guid}/Edit")]
    public async Task<IActionResult> UserEdit(Guid id, EditAdminUserViewModel model) {
        if (EffectiveOrg is not { } org) {
            model.Id = id;
            model.Error = "Selecione uma organização no menu superior para gerenciar usuários.";
            return View(model);
        }
        model.Id = id;
        if (!UserStatuses.Any(s => s == model.Status)) ModelState.AddModelError(nameof(model.Status), "Status inválido; selecione uma opção da lista.");
        if (!ModelState.IsValid) return View(await WithEditRoleOptions(model, org));
        try {
            var current = await userAdministration.GetAsync(org, id);
            model.Email = current.Email;
        }
        catch (NotFoundAppException) {
            model.Error = "Usuário não encontrado ou fora do seu escopo de acesso.";
            return View(model);
        }
        catch (Exception) {
            model.Error = "Não foi possível carregar o usuário. Tente novamente.";
            return View(model);
        }
        try {
            await userAdministration.UpdateAsync(org, id, new UpdateUserRequest(model.Name.Trim(), model.Phone));
            await userAdministration.SetRolesAsync(org, Actor, id, new UpdateUserRolesRequest([.. model.RoleCodes.Distinct(StringComparer.OrdinalIgnoreCase)]));
            await userAdministration.UpdateStatusAsync(org, Actor, id, new UpdateUserStatusRequest(model.Status));
            await audit.AddAsync(new AuditEntry(org, Actor, "admin.user.updated", "user", id.ToString(),
                $"Usuário {model.Email} atualizado (status: {model.Status}; papéis: {string.Join(",", model.RoleCodes.Distinct(StringComparer.OrdinalIgnoreCase))}).", module: "admin"));
            TempData["AdminNotice"] = "Usuário atualizado com sucesso.";
            return RedirectToAction(nameof(UserEdit), new { id });
        }
        catch (BusinessRuleAppException ex) {
            model.Error = ex.Message;
            return View(await WithEditRoleOptions(model, org));
        }
        catch (ValidationAppException ex) {
            model.Error = ex.Message;
            return View(await WithEditRoleOptions(model, org));
        }
        catch (ForbiddenAppException ex) {
            model.Error = ex.Message;
            return View(await WithEditRoleOptions(model, org));
        }
        catch (Exception) {
            model.Error = "Não foi possível salvar as alterações agora. Tente novamente.";
            return View(model);
        }
    }

    [HttpGet("Roles")]
    public async Task<IActionResult> Roles() {
        if (EffectiveOrg is not { } org)
            return View(new AdminRolesViewModel([], [], null, "Selecione uma organização no menu superior."));
        try {
            var roles = (await access.ListRolesAsync(org, CancellationToken.None))
                .Select(r => new AdminRoleRowViewModel(r.Id, r.Code, r.Name, r.Description, r.IsSystem, r.Status, r.UserCount, r.Permissions.Count)).ToList();
            IReadOnlyList<AdminModuleRowViewModel> modules = [];
            try {
                modules = (await access.ListModulesAsync(CancellationToken.None))
                    .Select(m => new AdminModuleRowViewModel(m.Code, m.Name, m.Status, m.Permissions.Count)).ToList();
            }
            catch (Exception) { /* módulos são informativos; falha não impede a lista de papéis */ }
            return View(new AdminRolesViewModel(roles, modules, (await organizations.GetAsync(org))?.Name));
        }
        catch (Exception) {
            return View(new AdminRolesViewModel([], [], null, "Não foi possível carregar os papéis. Tente novamente."));
        }
    }

    [HttpGet("Settings")]
    public async Task<IActionResult> Settings() {
        if (EffectiveOrg is not { } org)
            return View(new AdminSettingsFormModel { OrganizationName = "", Error = "Selecione uma organização no menu superior." });
        try {
            var record = await organizations.GetAsync(org);
            if (record is null)
                return View(new AdminSettingsFormModel { OrganizationName = "", Error = "Organização não encontrada para o contexto atual." });
            var settings = ReadSettings(await organizations.GetSettingsAsync(org));
            return View(new AdminSettingsFormModel {
                OrganizationName = record.Name,
                Plan = await PlanNameAsync(org),
                DiagnosticReminders = settings.GetBool("diagnostic_reminders"),
                ReportNotifications = settings.GetBool("report_notifications"),
                AnonymizeResponses = settings.GetBool("anonymize_responses"),
                Usage = await UsageRowsAsync(org)
            });
        }
        catch (Exception) {
            return View(new AdminSettingsFormModel { OrganizationName = "organização", Error = "Não foi possível carregar as configurações. Tente novamente." });
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Settings")]
    public async Task<IActionResult> Settings(AdminSettingsFormModel model) {
        if (EffectiveOrg is not { } org) {
            model.Error = "Selecione uma organização no menu superior.";
            return View(model);
        }
        try {
            var existing = ReadSettings(await organizations.GetSettingsAsync(org));
            existing["diagnostic_reminders"] = JsonSerializer.SerializeToElement(model.DiagnosticReminders);
            existing["report_notifications"] = JsonSerializer.SerializeToElement(model.ReportNotifications);
            existing["anonymize_responses"] = JsonSerializer.SerializeToElement(model.AnonymizeResponses);
            await organizations.UpsertSettingsAsync(org, existing);
            await audit.AddAsync(new AuditEntry(org, Actor, "admin.settings.updated", "settings", org.ToString(), "Preferências administrativas atualizadas.", module: "admin"));
            TempData["AdminNotice"] = "Configurações salvas.";
            return RedirectToAction(nameof(Settings));
        }
        catch (Exception) {
            model.Error = "Não foi possível salvar as configurações agora. Tente novamente.";
            return View(model);
        }
    }

    [HttpGet("Audit")]
    public async Task<IActionResult> Audit(string? search, DateOnly? from, DateOnly? to) {
        if (EffectiveOrg is not { } org)
            return View(new AdminAuditPageViewModel([], 0, search, from, to, null, "Selecione uma organização no menu superior."));
        string? orgName = null;
        List<AdminAuditRowViewModel> all = [];
        try {
            orgName = (await organizations.GetAsync(org))?.Name;
            all = (await audit.ListAdminAsync(org, 200)).Select(MapAudit).ToList();
        }
        catch (Exception) {
            return View(new AdminAuditPageViewModel([], 0, search, from, to, orgName, "Não foi possível carregar a auditoria. Tente novamente."));
        }
        IEnumerable<AdminAuditRowViewModel> filtered = all;
        if (!string.IsNullOrWhiteSpace(search)) {
            var term = search.Trim();
            filtered = filtered.Where(e => (e.Action != null && e.Action.Contains(term, StringComparison.OrdinalIgnoreCase))
                || (e.Message != null && e.Message.Contains(term, StringComparison.OrdinalIgnoreCase))
                || (e.EntityType != null && e.EntityType.Contains(term, StringComparison.OrdinalIgnoreCase))
                || (e.EntityId != null && e.EntityId.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }
        if (from is { } f) {
            var start = f.ToDateTime(TimeOnly.MinValue);
            filtered = filtered.Where(e => e.CreatedAt >= start);
        }
        if (to is { } t) {
            var end = t.ToDateTime(TimeOnly.MaxValue);
            filtered = filtered.Where(e => e.CreatedAt <= end);
        }
        return View(new AdminAuditPageViewModel(filtered.ToList(), all.Count, search, from, to, orgName));
    }

    private async Task<OrganizationDetailsFormModel> HydrateDetailsAsync(Guid id) {
        var org = await organizations.GetAsync(id) ?? throw new InvalidOperationException("Organização não encontrada.");
        return new OrganizationDetailsFormModel {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            Status = org.Status,
            Plan = await PlanNameAsync(id),
            Cnpj = org.Cnpj,
            CreatedAt = org.CreatedAt,
            Version = org.Version,
            PublicName = org.PublicName,
            Phone = org.Phone,
            Email = org.Email,
            TimeZone = org.TimeZone,
            LegalName = org.LegalName,
            Segment = org.Segment,
            Region = org.Region,
            City = org.City,
            State = org.State,
            PrimaryContactName = org.PrimaryContactName,
            Usage = await UsageRowsAsync(id),
            RecentEvents = await RecentAuditAsync(id)
        };
    }

    private async Task<string> PlanNameAsync(Guid org) {
        try {
            var planId = await plans.GetCurrentPlanIdAsync(org);
            if (planId is null) return "—";
            return (await plans.GetByIdAsync(planId))?.Name ?? "—";
        }
        catch (Exception) {
            return "—";
        }
    }

    private async Task<IReadOnlyList<SelectListItem>> PlanOptionsAsync() {
        try {
            var publicPlans = await plans.GetPublicPlansAsync();
            if (publicPlans.Count > 0)
                return publicPlans.OrderBy(p => p.DisplayOrder).Select(p => new SelectListItem(p.Name, p.Id)).ToList();
        }
        catch (Exception) { /* plano de leitura indisponível; usa catálogo estável abaixo */ }
        return new[] { "free", "start", "growth", "enterprise" }.Select(code => new SelectListItem(code, code)).ToList();
    }

    private async Task<IReadOnlyList<SelectListItem>> RoleOptionsAsync(Guid org) =>
        (await access.ListRolesAsync(org, CancellationToken.None)).Select(r => new SelectListItem($"{r.Name} ({r.Code})", r.Code)).ToList();

    private async Task<CreateAdminUserViewModel> WithRoleOptions(CreateAdminUserViewModel model, Guid org) {
        try { model.Roles = await RoleOptionsAsync(org); } catch (Exception) { /* opções ficam vazias; o erro de carregamento já é exibido */ }
        return model;
    }

    private async Task<EditAdminUserViewModel> WithEditRoleOptions(EditAdminUserViewModel model, Guid org) {
        try { model.Roles = await RoleOptionsAsync(org); } catch (Exception) { /* opções ficam vazias; o erro de carregamento já é exibido */ }
        return model;
    }

    private Task<IReadOnlyList<AdminUsageRowViewModel>> UsageRowsAsync(Guid org) =>
        Safe<AdminUsageRowViewModel>(async () => (await organizations.GetUsageAsync(org)).Select(u => new AdminUsageRowViewModel(u.Key, u.Period, u.Consumed, u.Limit, u.Percentage, u.Unlimited)).ToList());

    private Task<IReadOnlyList<AdminAuditRowViewModel>> RecentAuditAsync(Guid org) =>
        Safe<AdminAuditRowViewModel>(async () => (await audit.ListAdminAsync(org, 25)).Select(MapAudit).ToList());

    private static async Task<IReadOnlyList<T>> Safe<T>(Func<Task<IReadOnlyList<T>>> operation) where T : class {
        try { return await operation(); } catch (Exception) { return []; }
    }

    private static Dictionary<string, object?> ReadSettings(IReadOnlyList<OrganizationSettingRecord> records) {
        var raw = records.FirstOrDefault()?.Settings;
        if (string.IsNullOrWhiteSpace(raw)) return [];
        try {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw);
            if (parsed is null) return [];
            return parsed.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        }
        catch (JsonException) { return []; }
    }

    private static AdminAuditRowViewModel MapAudit(object row) {
        var r = (IDictionary<string, object>)row;
        return new AdminAuditRowViewModel(
            ToUtc(r["CreatedAt"]),
            AsString(r["severity"]) ?? "info",
            AsString(r["action"]) ?? "—",
            AsString(r["EntityType"]),
            r["EntityId"]?.ToString(),
            AsString(r["message"]),
            AsString(r["UserAgent"]));
    }

    private static string? AsString(object? value) => value as string;

    private static DateTime ToUtc(object? value) => value switch {
        DateTime dateTime => dateTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.ToUniversalTime(),
        DateTimeOffset offset => offset.UtcDateTime,
        _ => DateTime.MinValue
    };
}

file static class OrganizationSettingExtensions {
    public static bool GetBool(this IReadOnlyDictionary<string, object?> settings, string key) =>
        settings.TryGetValue(key, out var value) && value is JsonElement { ValueKind: JsonValueKind.True };
}
