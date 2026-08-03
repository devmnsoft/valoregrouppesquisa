using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;
using Valora.Web.Models;

namespace Valora.Web.Controllers;

[Authorize]
[ApiController]
[Route("bff/account")]
public sealed class BffAccountController(BffAuthenticationService authentication) : ControllerBase
{
    [HttpGet("context")]
    public async Task<IActionResult> Context(CancellationToken cancellationToken)
    {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        if (session is null) return Unauthorized();

        var permissions = User.FindAll("permission")
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToArray();

        var user = session.SafeSession.User;
        var organization = session.SafeSession.Organization;
        var plan = session.SafeSession.Plan;
        return Ok(new AccountContextResponse(
            user.Id, user.Name, user.Email, Initials(user.Name), user.Role, [user.Role],
            organization?.Id, organization?.TradeName ?? organization?.Name, null,
            plan?.Id, plan?.Name, User.FindFirst("subscription_status")?.Value ?? (plan is null ? "missing" : "active"),
            permissions, Claims("capability"), Claims("scope")));
    }

    private string[] Claims(string type) => User.FindAll(type)
        .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(part => char.ToUpperInvariant(part[0])));
}
