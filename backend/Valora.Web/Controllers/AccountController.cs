using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Valora.Web.Controllers;

[AllowAnonymous]
public sealed class AccountController(ILogger<AccountController> logger) : Controller
{
    [HttpGet("/Login")]
    [HttpGet("/Account/Login")]
    public IActionResult Login()
    {
        try
        {
            ViewData["Title"] = "Login";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar AccountController.Login no Valora.Web.");
            throw;
        }
    }
    [HttpGet("/cadastro")]
    [HttpGet("/comecar-teste-gratis")]
    public IActionResult Register([FromQuery] string? plan = null)
    {
        try
        {
            ViewData["Title"] = "Criar conta";
            ViewData["SelectedPlan"] = plan;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar AccountController.Register no Valora.Web.");
            throw;
        }
    }
    public IActionResult ForgotPassword()
    {
        try
        {
            ViewData["Title"] = "ForgotPassword";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar AccountController.ForgotPassword no Valora.Web.");
            throw;
        }
    }
    public IActionResult ResetPassword()
    {
        try
        {
            ViewData["Title"] = "ResetPassword";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar AccountController.ResetPassword no Valora.Web.");
            throw;
        }
    }
}
