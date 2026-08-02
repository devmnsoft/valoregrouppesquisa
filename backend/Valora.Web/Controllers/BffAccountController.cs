using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

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

        return Ok(new
        {
            user = session.SafeSession.User,
            organization = session.SafeSession.Organization,
            subscription = session.SafeSession.Plan,
            permissions
        });
    }
}
