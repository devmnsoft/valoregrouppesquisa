using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

public sealed class ResultsController(ILogger<ResultsController> logger) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Resultados";
        return View();
    }

    [Route("r/{responseId}")]
    [Route("public/results/{responseId}")]
    [Route("resultado/{responseId}")]
    public IActionResult Public(string responseId)
    {
        try
        {
            ViewData["Title"] = "Public";
            ViewData["ResponseId"] = responseId;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar ResultsController.Public no Valora.Web.");
            throw;
        }
    }

    [Route("resultado/{responseId}/email")]
    public IActionResult Email(string responseId)
    {
        try
        {
            ViewData["Title"] = "Enviar resultado por e-mail";
            ViewData["ResponseId"] = responseId;
            return View("Public");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar ResultsController.Email no Valora.Web.");
            throw;
        }
    }

    public IActionResult Details(string id)
    {
        try
        {
            ViewData["Title"] = "Details";
            ViewData["ResponseId"] = id;
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar ResultsController.Details no Valora.Web.");
            throw;
        }
    }
}
