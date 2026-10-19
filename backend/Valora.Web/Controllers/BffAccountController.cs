using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Models;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[Authorize]
[ApiController]
[Route("bff/account")]
public sealed class BffAccountController(BffAuthenticationService authentication) : ControllerBase {
    [HttpGet("context")]
    public async Task<IActionResult> Context(CancellationToken cancellationToken) {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        if (session is null) return Unauthorized();

        var permissions = User.FindAll("permission")
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToArray();

        var user = session.SafeSession.User;
        var isGlobal = session.SafeSession.AccessContext.IsGlobalAdministrator;
        var selectedOrgId = session.SafeSession.AccessContext.SelectedOrganizationId;
        var organization = session.SafeSession.Organization;

        Guid? effectiveOrgId;
        string? effectiveOrgName;

        if (isGlobal) {
            if (selectedOrgId is null || selectedOrgId == Guid.Empty) {
                effectiveOrgId = null;
                effectiveOrgName = null;
            }
            else {
                effectiveOrgId = selectedOrgId;
                effectiveOrgName = organization?.TradeName ?? organization?.Name ?? "Organização selecionada";
            }
        }
        else {
            effectiveOrgId = organization?.Id ?? session.SafeSession.AccessContext.OrganizationId;
            effectiveOrgName = organization?.TradeName ?? organization?.Name;
        }

        var plan = session.SafeSession.Plan;
        return Ok(new AccountContextResponse(
            user.Id, user.Name, user.Email, Initials(user.Name), user.Role, [user.Role],
            effectiveOrgId, effectiveOrgName, null,
            plan?.Id, plan?.Name, User.FindFirst("subscription_status")?.Value ?? (plan is null ? "missing" : "active"),
            permissions, Claims("capability"), Claims("scope")));
    }

    private string[] Claims(string type) => User.FindAll(type)
        .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(part => char.ToUpperInvariant(part[0])));
}
