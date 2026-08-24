using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Valora.Web.Controllers;

[AllowAnonymous]
public sealed class HomeController(ILogger<HomeController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Home";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar HomeController.Index no Valora.Web.");
            throw;
        }
    }
    [HttpGet("/error/{statusCode:int}")]
    public IActionResult Error(int statusCode = 500)
    {
        var safeStatus = statusCode is 400 or 401 or 403 or 404 or 500 ? statusCode : 500;
        if (safeStatus == 500 && HttpContext.Response.StatusCode < 400)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
        ViewData["Title"] = $"Erro {safeStatus}";
        ViewData["StatusCode"] = safeStatus;
        ViewData["CorrelationId"] = HttpContext.TraceIdentifier;
        return View("Error");
    }
}
