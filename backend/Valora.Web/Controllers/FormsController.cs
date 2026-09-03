using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class FormsController(ILogger<FormsController> logger) : Controller
{
    public IActionResult Index()
    {
        try
        {
            ViewData["Title"] = "Forms";
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar FormsController.Index no Valora.Web.");
            throw;
        }
    }

    [HttpGet("Forms/Create")]
    public IActionResult Create() => Redirect("/Forms?intent=create");

    [HttpGet("Forms/{formId:guid}/Builder")]
    public IActionResult Builder(Guid formId)
    {
        ViewData["Title"] = "Estúdio de Diagnósticos";
        return View(formId);
    }

    [HttpGet("Forms/{formId:guid}/Preview")]
    public IActionResult Preview(Guid formId) => Redirect($"/Forms/{formId:D}/Builder?preview=true");
}
