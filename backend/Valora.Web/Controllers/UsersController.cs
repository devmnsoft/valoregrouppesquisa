using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Valora.Web.Controllers;

[Authorize]
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

    [HttpGet("Users/Roles")]
    public IActionResult Roles() => Redirect("/Users#roles-list");

    [HttpGet("Users/Permissions")]
    public IActionResult Permissions() => Redirect("/Users#permissions-list");
}
