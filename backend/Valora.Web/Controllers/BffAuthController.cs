using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Web.Services.Bff;

namespace Valora.Web.Controllers;

[ApiController]
[AutoValidateAntiforgeryToken]
[Route("bff/auth")]
public sealed class BffAuthController(BffAuthenticationService authentication, IBffApiClient api) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] object request, CancellationToken cancellationToken) =>
        Ok(await authentication.SignInAsync(HttpContext, "/api/v1/auth/login", request, cancellationToken));

    [AllowAnonymous, HttpPost("register-company")]
    public async Task<IActionResult> Register([FromBody] object request, CancellationToken cancellationToken) =>
        Ok(await authentication.SignInAsync(HttpContext, "/api/v1/auth/register-company", request, cancellationToken));

    [AllowAnonymous, HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot([FromBody] object request, CancellationToken cancellationToken)
    {
        await api.PostAsync("/api/v1/auth/forgot-password", request, null, cancellationToken);
        return Accepted(new { ok = true });
    }

    [AllowAnonymous, HttpPost("reset-password")]
    public async Task<IActionResult> Reset([FromBody] object request, CancellationToken cancellationToken)
    {
        await api.PostAsync("/api/v1/auth/reset-password", request, null, cancellationToken);
        return Ok(new { ok = true });
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authentication.SignOutAsync(HttpContext, cancellationToken);
        return NoContent();
    }

    [Authorize, HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var session = await authentication.RefreshAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(session);
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(session.SafeSession);
    }

    [Authorize, HttpGet("sessions")]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken)
    {
        var session = await authentication.GetAsync(HttpContext, cancellationToken);
        return session is null ? Unauthorized() : Ok(new[] { session.SafeSession });
    }
}
