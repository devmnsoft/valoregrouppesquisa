using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class AccountController(ILogger<AccountController> logger) : Controller
{
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
    public IActionResult Register()
    {
        try
        {
            ViewData["Title"] = "Register";
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
