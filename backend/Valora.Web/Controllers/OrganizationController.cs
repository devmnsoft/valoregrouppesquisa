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

    [HttpGet("Organization/Structure")]
    public IActionResult Structure() => Redirect("/Organization#org-structure");

    [HttpGet("Onboarding")]
    public IActionResult Onboarding() => Redirect("/Organization#org-onboarding");
}
