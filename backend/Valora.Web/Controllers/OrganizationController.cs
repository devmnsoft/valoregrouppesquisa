using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class OrganizationController(ILogger<OrganizationController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Organization";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar OrganizationController.Index no Valora.Web.");
            throw;
        }
    }
}
