using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class OperationsController(ILogger<OperationsController> logger) : Controller
{
    [Route("Operations")]
    [Route("Operations/Index")]
    public IActionResult Index() => Render("Saúde operacional");

    [Route("Operations/Health")]
    public IActionResult Health() => Render("Health checks operacionais");

    [Route("Operations/Version")]
    public IActionResult Version() => Render("Versão e build");

    [Route("Operations/Checks")]
    public IActionResult Checks() => Render("Checklist operacional");

    private IActionResult Render(string title)
    {
        logger.LogInformation("Operations page rendered. Title={Title}", title);
        ViewData["Title"] = title;
        return View("Index");
    }
}
