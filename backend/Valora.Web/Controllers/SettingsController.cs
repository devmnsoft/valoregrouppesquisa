using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;
using Valora.Web.Models.ViewModels;

namespace Valora.Web.Controllers;

[Authorize]
public sealed class SettingsController(
    ICurrentRequestContext currentRequest,
    IOrganizationRepository organizationRepo,
    IConfiguration configuration,
    ILogger<SettingsController> logger) : Controller {

    [HttpGet("/Settings")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) {
        try {
            var ctx = currentRequest.GetCurrent();
            var name = User.Identity?.Name ?? "Usuário Valora";
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? "usuario@valora.com.br";
            var role = ctx.Roles.FirstOrDefault() ?? User.FindFirst(ClaimTypes.Role)?.Value ?? "Operador";

            var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(p => char.ToUpperInvariant(p[0])));

            if (string.IsNullOrWhiteSpace(initials)) initials = "VA";

            var profile = new UserProfileSettingsViewModel(
                ctx.UserId ?? Guid.Empty,
                name,
                email,
                role,
                initials);

            OrganizationSettingsViewModel? orgModel = null;
            if (ctx.EffectiveOrganizationId is { } orgId && orgId != Guid.Empty) {
                var orgRecord = await organizationRepo.GetAsync(orgId, cancellationToken);
                var orgName = orgRecord is not null && !string.IsNullOrWhiteSpace(orgRecord.Name)
                    ? orgRecord.Name
                    : "Organização Ativa";

                orgModel = new OrganizationSettingsViewModel(
                    orgId,
                    orgName,
                    "Plano Corporativo Enterprise",
                    string.IsNullOrWhiteSpace(ctx.SubscriptionStatus) ? "Ativo" : ctx.SubscriptionStatus,
                    ctx.Modules);
            }

            var sessionMinutes = configuration.GetValue("Authentication:SessionMinutes", 30);
            var security = new SecuritySettingsViewModel(
                sessionMinutes,
                "Valora.Session (Cookie Criptografado DPAPI/Redis)",
                CsrfProtectionActive: true,
                MultiTenantIsolationActive: true);

            return View(new SettingsIndexViewModel(profile, orgModel, security));
        }
        catch (Exception ex) {
            logger.LogError(ex, "Falha ao carregar configurações do usuário");
            TempData["ErrorMessage"] = "Não foi possível carregar as preferências no momento.";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
