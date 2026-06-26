using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class AuthController(AuthService auth, IUserRepository users) : ControllerBase
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
    public IActionResult Forgot()
    {
        return Accepted(new
        {
            ok = true,
            message = "Fluxo de recuperação será integrado ao e-mail transacional na próxima fase."
        });
    }

    [Authorize]
    [HttpGet("/me")]
    public async Task<IActionResult> Me()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await users.GetAsync(id));
    }
}
