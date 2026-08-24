using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [HttpGet("/teste-gratis")]
    [HttpGet("/Account/Register")]
    public IActionResult Register([FromQuery] string? plan = null)
    {
        try
        {
            ViewData["Title"] = "Começar teste grátis";
            ViewData["SelectedPlan"] = plan;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar AccountController.Register no Valora.Web.");
            throw;
        }
    }
    [HttpGet("/recuperar-senha")]
    [HttpGet("/Account/ForgotPassword")]
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
    [HttpGet("/redefinir-senha")]
    [HttpGet("/Account/ResetPassword")]
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
