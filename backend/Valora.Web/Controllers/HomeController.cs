using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[AllowAnonymous]
public sealed class HomeController(ILogger<HomeController> logger) : Controller {
    public IActionResult Index() {
        try {
            ViewData["Title"] = "Home";
            return View();
        }
        catch (Exception ex) {
            logger.LogError(ex, "Falha ao renderizar HomeController.Index no Valora.Web.");
            throw;
        }
    }
    // Status-code re-execution keeps the original HTTP method, including rejected POSTs.
    [Route("/error/{statusCode:int}")]
    public IActionResult Error(int statusCode = 500) {
        var safeStatus = statusCode is 400 or 401 or 403 or 404 or 500 ? statusCode : 500;
        // Re-execution preserves the original status, but a direct visit to /error/{code}
        // would otherwise return 200.  Always keep the HTTP contract aligned with the
        // friendly page so monitoring, clients and assistive technology see the truth.
        HttpContext.Response.StatusCode = safeStatus;
        ViewData["Title"] = $"Erro {safeStatus}";
        ViewData["StatusCode"] = safeStatus;
        ViewData["CorrelationId"] = HttpContext.TraceIdentifier;
        return View("Error");
    }
}
