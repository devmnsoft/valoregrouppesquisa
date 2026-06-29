using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class UsersController(ILogger<UsersController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Users";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar UsersController.Index no Valora.Web.");
            throw;
        }
    }
}
