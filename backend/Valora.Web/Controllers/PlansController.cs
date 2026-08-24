using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[AllowAnonymous]
public sealed class PlansController(ILogger<PlansController> logger) : Controller
{
    [HttpGet("/Plans")]
    [HttpGet("/planos")]
    [HttpGet("/Pricing")]
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Plans";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar PlansController.Index no Valora.Web.");
            throw;
        }
    }
}
