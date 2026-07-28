using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.Security;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class AuthController(AuthService auth, IUserRepository users, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("/api/v1/auth/register-company")]
    public async Task<IActionResult> Register(RegisterCompanyRequest request)
    {
        return Ok(await auth.RegisterCompanyAsync(request));
    }

    [HttpPost("/api/v1/auth/login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        return Ok(await auth.LoginAsync(request));
    }

    [HttpPost("/api/v1/auth/refresh")]
    public async Task<ActionResult<AuthenticationResult>> Refresh(RefreshRequest request) => Ok(await auth.RefreshAsync(request));

    [Authorize, HttpPost("/api/v1/auth/logout")]
    public async Task<IActionResult> Logout(LogoutRequest request)
    {
        await auth.LogoutAsync(CurrentUserId(), request);
        return NoContent();
    }

    [Authorize, HttpPost("/api/v1/auth/logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        await auth.LogoutAllAsync(CurrentUserId());
        return NoContent();
    }

    [Authorize, HttpGet("/api/v1/auth/sessions")]
    public async Task<ActionResult<IReadOnlyList<SessionDto>>> Sessions() => Ok(await auth.ListSessionsAsync(CurrentUserId()));

    [Authorize, HttpDelete("/api/v1/auth/sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId)
    {
        await auth.RevokeSessionAsync(CurrentUserId(), sessionId);
        return NoContent();
    }

    [HttpPost("/api/v1/auth/forgot-password")]
    public async Task<IActionResult> Forgot(ForgotPasswordRequest request)
    {
        try
        {
            await auth.ForgotPasswordAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return Accepted(new { ok = true, message = "Se o e-mail estiver cadastrado, enviaremos instruções de recuperação." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha controlada no forgot-password. Email={Email}", LogSanitizer.MaskEmail(request.Email));
            return Accepted(new { ok = true, message = "Se o e-mail estiver cadastrado, enviaremos instruções de recuperação." });
        }
    }

    [HttpPost("/api/v1/auth/reset-password")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request)
    {
        await auth.ResetPasswordAsync(request);
        return Ok(new { ok = true, message = "Senha redefinida com sucesso." });
    }

    [Authorize]
    [HttpGet("/me")]
    [HttpGet("/api/v1/auth/me")]
    public async Task<IActionResult> Me()
    {
        var id = CurrentUserId();
        return Ok(await users.GetAsync(id));
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
