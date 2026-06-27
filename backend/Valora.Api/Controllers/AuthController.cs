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
    [HttpPost("/auth/register-company")]
    public async Task<IActionResult> Register(RegisterCompanyRequest request)
    {
        return Ok(await auth.RegisterCompanyAsync(request));
    }

    [HttpPost("/auth/login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        return Ok(await auth.LoginAsync(request));
    }

    [HttpPost("/auth/forgot-password")]
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

    [HttpPost("/auth/reset-password")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request)
    {
        await auth.ResetPasswordAsync(request);
        return Ok(new { ok = true, message = "Senha redefinida com sucesso." });
    }

    [Authorize]
    [HttpGet("/me")]
    public async Task<IActionResult> Me()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await users.GetAsync(id));
    }
}
