using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Valora.Web.Models;

namespace Valora.Web.Controllers;

public sealed class ResultsController(ILogger<ResultsController> logger) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Resultados";
        return View();
    }

    [AllowAnonymous]
    [Route("public/results/{token}")]
    [Route("public/results/{token}/executive")]
    [Route("public/results/{token}/report")]
    [Route("resultado/{token}")]
    public IActionResult Public(string token)
    {
        try
        {
            ViewData["Title"] = "Public";
            var model = new PublicResultExperienceViewModel { Token = token };
            if (!TryValidateModel(model)) return NotFound();
            return View(model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar ResultsController.Public no Valora.Web.");
            throw;
        }
    }

    [AllowAnonymous]
    [Route("resultado/{responseId}/email")]
    public IActionResult Email(string responseId)
    {
        try
        {
            ViewData["Title"] = "Enviar resultado por e-mail";
            return View("Public", new PublicResultExperienceViewModel { Token = responseId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao renderizar ResultsController.Email no Valora.Web.");
            throw;
        }
    }

    public IActionResult Details(string id)
    {
        var model = new ResultDetailsViewModel { ResponseId = id };
        if (!TryValidateModel(model)) return NotFound();

        ViewData["Title"] = "Detalhes do resultado";
        return View(model);
    }
}
